using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// One place that owns the multiplayer session lifecycle:
/// Unity Services init → anonymous sign-in → create/join a Session with a Relay
/// network (join codes) → NGO host/client starts automatically via the
/// Multiplayer Services SDK. WebGL talks to Relay over secure WebSockets.
/// </summary>
public static class NetSession
{
    public static ISession Session { get; private set; }
    public static NetAvatar LocalAvatar;

    // Scene refs resolved by NetBoot.
    public static Transform LocalRig;
    public static Transform LocalHead;
    public static Movement LocalMovement;
    public static SeedUseController LocalSeedUse;

    public static bool InSession => Session != null;
    public static string JoinCode => Session?.Code ?? "";
    public static event Action Changed;

    const int MaxPlayers = 8;

    static bool _servicesReady;
    static GameObject _networkManagerGO;

    public static async Task<string> HostAsync()
    {
        await EnsureServicesAsync();
        EnsureNetworkManager();

        var options = new SessionOptions
        {
            MaxPlayers = MaxPlayers,
            IsPrivate = false,
        }.WithRelayNetwork();

        Session = await MultiplayerService.Instance.CreateSessionAsync(options);
        HookSessionEvents();
        Changed?.Invoke();
        return Session.Code;
    }

    public static async Task JoinAsync(string code)
    {
        await EnsureServicesAsync();
        EnsureNetworkManager();

        Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
        HookSessionEvents();
        Changed?.Invoke();
    }

    public static async Task LeaveAsync()
    {
        var s = Session;
        Session = null;
        try { if (s != null) await s.LeaveAsync(); }
        catch (Exception e) { Debug.LogWarning($"[NetSession] Leave: {e.Message}"); }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
        if (_networkManagerGO != null)
        {
            UnityEngine.Object.Destroy(_networkManagerGO);
            _networkManagerGO = null;
        }
        Changed?.Invoke();
    }

    static void HookSessionEvents()
    {
        try
        {
            Session.PlayerJoined += _ => Changed?.Invoke();
            Session.PlayerLeaving += _ => Changed?.Invoke();
            Session.Deleted += () => { Session = null; Changed?.Invoke(); };
            Session.RemovedFromSession += () => { Session = null; Changed?.Invoke(); };
        }
        catch (Exception e)
        {
            // Events are convenience only; the HUD also polls.
            Debug.LogWarning($"[NetSession] Session events unavailable: {e.Message}");
        }
    }

    static async Task EnsureServicesAsync()
    {
        if (_servicesReady) return;
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _servicesReady = true;
    }

    static void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null) return;

        var prefab = Resources.Load<GameObject>("Net/NetAvatar");
        if (prefab == null)
            throw new InvalidOperationException(
                "Net/NetAvatar prefab missing from Resources — run NetAvatarPrefabBuilder (it runs automatically in the WebGL build).");

        _networkManagerGO = new GameObject("NetworkManager (runtime)");
        UnityEngine.Object.DontDestroyOnLoad(_networkManagerGO);

        var transport = _networkManagerGO.AddComponent<UnityTransport>();
#if UNITY_WEBGL && !UNITY_EDITOR
        transport.UseWebSockets = true; // browsers cannot do UDP; Relay over wss
#endif

        var nm = _networkManagerGO.AddComponent<NetworkManager>();
        nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            PlayerPrefab = prefab,
        };
    }
}
