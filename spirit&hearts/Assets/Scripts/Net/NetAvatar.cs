using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The networked presence of one player. Spawned per-client by NGO (player prefab).
/// - Owner: invisible, follows the local PlayerRig every frame; forwards local seed
///   launches to everyone else; broadcasts newly-lit lights (1 Hz poll).
/// - Remote: visible bird proxy driven by ClientNetworkTransform interpolation;
///   replays incoming seed launches through the local SeedUseController so the
///   seed flies with the exact same physics on every screen.
/// </summary>
public class NetAvatar : NetworkBehaviour
{
    public static readonly List<NetAvatar> All = new List<NetAvatar>();

    static readonly Color[] Palette =
    {
        new Color(1.00f, 0.85f, 0.35f), // gold
        new Color(0.45f, 0.75f, 1.00f), // sky
        new Color(1.00f, 0.45f, 0.55f), // rose
        new Color(0.55f, 1.00f, 0.65f), // mint
        new Color(0.80f, 0.55f, 1.00f), // violet
        new Color(1.00f, 0.65f, 0.30f), // ember
    };

    const float SeedHitRadius = 5f;
    const float SeedKnockback = 12f;

    float _lightPollTimer;
    Dictionary<string, LightController> _lightsById;
    readonly HashSet<string> _knownLit = new HashSet<string>();

    public override void OnNetworkSpawn()
    {
        All.Add(this);

        if (IsOwner)
        {
            NetSession.LocalAvatar = this;
            // The owner's avatar is a transmitter, not a visual — the local player
            // already sees the world first-person through their own rig.
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            SeedUseController.OnLocalSeedLaunched += OnLocalSeedLaunched;
        }
        else
        {
            Colorize();
        }

        SessionHud.Instance?.Notify($"{(IsOwner ? "You are" : "A friend")} in the sky ({All.Count} flying)");
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        if (IsOwner)
        {
            SeedUseController.OnLocalSeedLaunched -= OnLocalSeedLaunched;
            if (NetSession.LocalAvatar == this) NetSession.LocalAvatar = null;
        }
        SessionHud.Instance?.Notify($"A bird left the sky ({All.Count} flying)");
    }

    void Colorize()
    {
        var color = Palette[(int)(OwnerClientId % (ulong)Palette.Length)];
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_Color", color);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);
    }

    void Update()
    {
        if (!IsOwner) return;

        var rig = NetSession.LocalRig;
        var head = NetSession.LocalHead;
        if (rig != null)
        {
            var rot = head != null ? head.rotation : rig.rotation;
            transform.SetPositionAndRotation(rig.position, rot);
        }

        PollLights();
        DetectIncomingSeedHits();
    }

    // ---- Seeds ----------------------------------------------------------

    void OnLocalSeedLaunched(Vector3 origin, Vector3 dir)
    {
        LaunchSeedRpc(origin, dir);
    }

    [Rpc(SendTo.NotMe)]
    void LaunchSeedRpc(Vector3 origin, Vector3 dir)
    {
        var suc = NetSession.LocalSeedUse;
        if (suc == null) return;
        suc.SpawnRemoteCourier(origin, dir);

        // Mark the newest courier as remote-fired so it can bonk the local player.
        var marker = FindNewestCourierAt(origin);
        if (marker != null && marker.GetComponent<NetSeedMarker>() == null)
        {
            var m = marker.AddComponent<NetSeedMarker>();
            m.shooterClientId = OwnerClientId;
        }
    }

    static GameObject FindNewestCourierAt(Vector3 origin)
    {
        SeedCourier best = null;
        float bestDist = 3f;
        foreach (var c in FindObjectsByType<SeedCourier>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(c.transform.position, origin);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best ? best.gameObject : null;
    }

    void DetectIncomingSeedHits()
    {
        var head = NetSession.LocalHead;
        var movement = NetSession.LocalMovement;
        if (head == null) return;

        foreach (var marker in NetSeedMarker.Active)
        {
            if (marker == null || marker.consumed) continue;
            if (Vector3.Distance(marker.transform.position, head.position) > SeedHitRadius) continue;

            marker.consumed = true;
            Vector3 knockDir = marker.Velocity.sqrMagnitude > 0.01f
                ? marker.Velocity.normalized
                : (head.position - marker.transform.position).normalized;
            movement?.AddExternalImpulse(knockDir * SeedKnockback);
            SessionHud.Instance?.Notify("Bonk! A friend's seed got you");
            SeedHitRpc(marker.shooterClientId);
            Destroy(marker.gameObject);
        }
    }

    [Rpc(SendTo.Everyone)]
    void SeedHitRpc(ulong shooterClientId)
    {
        if (NetworkManager != null && NetworkManager.LocalClientId == shooterClientId && !IsOwner)
            SessionHud.Instance?.Notify("Direct hit! You bonked a friend");
    }

    // ---- Light sync (safety net — both sides also simulate the seeds) ----

    void PollLights()
    {
        _lightPollTimer -= Time.deltaTime;
        if (_lightPollTimer > 0f) return;
        _lightPollTimer = 1f;

        if (_lightsById == null)
        {
            _lightsById = new Dictionary<string, LightController>();
            foreach (var lc in FindObjectsByType<LightController>(FindObjectsSortMode.None))
            {
                var pid = lc.GetComponent<PersistentId>() ?? lc.GetComponentInParent<PersistentId>();
                if (pid != null) _lightsById[pid.Id] = lc;
            }
            foreach (var kv in _lightsById)
                if (kv.Value.isLit) _knownLit.Add(kv.Key);
            return; // first pass just builds the baseline
        }

        foreach (var kv in _lightsById)
        {
            if (kv.Value == null || !kv.Value.isLit || _knownLit.Contains(kv.Key)) continue;
            _knownLit.Add(kv.Key);
            LightLitRpc(kv.Key);
        }
    }

    [Rpc(SendTo.NotMe)]
    void LightLitRpc(FixedString64Bytes lightId)
    {
        var local = NetSession.LocalAvatar;
        if (local == null || local._lightsById == null) return;

        string id = lightId.ToString();
        local._knownLit.Add(id);
        if (local._lightsById.TryGetValue(id, out var lc) && lc != null && !lc.isLit)
        {
            lc.isLit = true;
            Object.FindFirstObjectByType<AmbientLightManager>()?.UpdateAmbientLight();
        }
    }
}
