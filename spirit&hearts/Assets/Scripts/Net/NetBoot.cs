using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

/// <summary>
/// Flatscreen (WebGL / no-headset) boot path. The shipped game is VR-first:
/// world-space menus clicked with XR rays, a tutorial that expects hand poses.
/// On WebGL none of that is reachable, so this bootstrap skips straight to free
/// flight with the classic keyboard controls and raises the session HUD.
///
/// Runs automatically in WebGL players; in the editor it only takes over when
/// no XR device is active, so headset testing is untouched.
/// </summary>
public static class NetBoot
{
    const int StartingSeeds = 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Runner().StartGlobal();
#elif UNITY_EDITOR
        Runner().StartGlobal();
#endif
    }

    static IEnumerator Runner()
    {
        // Let the scene's managers finish their Start() work (tutorial autostart,
        // menu placement, XR init attempts).
        yield return null;
        yield return new WaitForSeconds(0.75f);

#if UNITY_EDITOR
        if (XRSettings.isDeviceActive) yield break; // headset session — stay out of the way
#endif

        // HUD and mouse input come FIRST — if any of the VR-bypass steps below
        // throws, the session menu must already be on screen.
        Try("mouse UI input", EnsureMouseUiInput);
        Try("session HUD", SessionHud.Ensure);

        var playerGO = GameObject.FindWithTag("MainPlayer");
        if (playerGO == null)
        {
            Debug.LogWarning("[NetBoot] No MainPlayer in this scene; flatscreen boot partial.");
            yield break;
        }

        NetSession.LocalRig = playerGO.transform;
        NetSession.LocalMovement = playerGO.GetComponent<Movement>();
        NetSession.LocalSeedUse = playerGO.GetComponent<SeedUseController>();
        NetSession.LocalHead = NetSession.LocalMovement != null ? NetSession.LocalMovement.head : null;

        // Straight into free flight: skip the VR menu and the tutorial gates.
        Try("skip tutorial", () => playerGO.GetComponent<TutorialManager>()?.SkipTutorial());
        Try("begin game", () =>
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu?.BeginGame();
        });
        // Belt and braces: if BeginGame threw halfway, force the VR canvases off.
        Try("hide VR canvases", () =>
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.name.Contains("Menu"))
                    canvas.gameObject.SetActive(false);
        });

        // Ammo for seed-shooting sessions (normally picked up in the world).
        Try("starter seeds", () =>
        {
            var inv = playerGO.GetComponent<SeedInventory>();
            if (inv != null)
                for (int i = 0; i < StartingSeeds; i++) inv.AddSeed();
        });

        Debug.Log("[NetBoot] Flatscreen boot complete.");
    }

    static void Try(string step, System.Action action)
    {
        try
        {
            action();
            Debug.Log($"[NetBoot] {step}: ok");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetBoot] {step} failed: {e.Message}");
        }
    }

    /// <summary>The scene EventSystem is XR-flavoured; give it a plain mouse module.</summary>
    static void EnsureMouseUiInput()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem (flatscreen)");
            es = go.AddComponent<EventSystem>();
        }

        bool hasStandalone = es.GetComponent<StandaloneInputModule>() != null;
        foreach (var module in es.GetComponents<BaseInputModule>())
            if (!(module is StandaloneInputModule))
                module.enabled = false;
        if (!hasStandalone)
            es.gameObject.AddComponent<StandaloneInputModule>();
    }

    // Coroutines need a MonoBehaviour host; static bootstraps don't have one.
    class Host : MonoBehaviour { }

    static void StartGlobal(this IEnumerator routine)
    {
        var go = new GameObject("NetBoot");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<Host>().StartCoroutine(routine);
    }
}
