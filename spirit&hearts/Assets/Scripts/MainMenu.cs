using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelRoot;
    public GameObject panelSettings;
    public GameObject panelPause;

    [Header("Fade")]
    public Image fader;
    [Range(0.1f, 3f)] public float fadeDuration = 1.0f;

    [Header("Placement")]
    public Transform xrOrigin;
    public float distanceFromHead = 2f;
    public Vector3 menuOffset = new Vector3(0, -0.1f, 0);

    [Header("Controls")]
    public KeyCode pauseKey = KeyCode.Escape;

    private TutorialManager tutorialManager;
    private Movement movement;
    private Camera cam;
    private bool isPaused = false;
    private bool settingsFromPause = false;
    private bool gameBegun = false;

    [Header("Placement Lock")]
    public bool lockDistanceFromBoot = true;
    private float lockedDistance;
    private Quaternion rotationOffsetFromFacing;


    void Awake()
    {
        ShowRoot();
        if (fader != null) SetAlpha(fader, 0f);
    }

    void Start()
    {
        cam = Camera.main;

        var player = GameObject.FindGameObjectWithTag("MainPlayer");
        tutorialManager = player.GetComponent<TutorialManager>();
        movement = player.GetComponent<Movement>();

        // capture fixed distance at boot (or use distanceFromHead)
        if (lockDistanceFromBoot && cam)
            lockedDistance = Vector3.Distance(transform.position, cam.transform.position);
        else
            lockedDistance = distanceFromHead;

        // capture your current angle as an offset from a "face player (yaw only)" basis
        rotationOffsetFromFacing = ComputeRotationOffset();

        // place once for boot menu
        TryPlaceInFrontOfHead();

        // boot menu state...
        panelRoot.SetActive(true);
        panelPause.SetActive(false);
        panelSettings.SetActive(false);
        isPaused = false;
        if (movement) movement.enabled = true;
    }


    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (!gameBegun) return;

            if (!isPaused)
            {
                PauseAndShowPause();
            }
            else
            {
                if (IsOnPause())
                    ResumeFromPause();
                else
                    ShowPause();
            }
        }
    }


    private void HandleEscape()
    {
        if (isPaused)
        {
            if (IsOnPause())
            {
                ResumeFromPause();
            }
            else
            {
                ShowPause();
            }
        }
        else
        {
            if (IsAnyMenuVisibleExceptPause())
            {
                if (IsOnSettings()) ShowRoot();
            }
            else
            {
                PauseAndShowPause();
            }
        }
    }


    // in MainMenu
    private void PauseAndShowPause()
    {
        isPaused = true;
        if (movement != null) movement.enabled = false;
        PauseState.SetPaused(true);
        TryPlaceInFrontOfHead();
        ShowPause();
    }

    private void ResumeFromPause()
    {
        isPaused = false;
        HideAllPanels();
        if (movement != null) movement.enabled = true;
        PauseState.SetPaused(false);
    }

    public void ShowRoot()
    {
        panelRoot.SetActive(true);
        panelPause.SetActive(false);
        panelSettings.SetActive(false);
    }

    public void ShowSettings()
    {
        panelRoot.SetActive(false);
        panelPause.SetActive(false);
        panelSettings.SetActive(true);
    }

    public void ShowPause()
    {
        panelRoot.SetActive(false);
        panelSettings.SetActive(false);
        panelPause.SetActive(true);
    }

    public void OpenSettings()
    {
        settingsFromPause = isPaused || panelPause.activeSelf;
        ShowSettings();
    }

    public void BeginGame()
    {
        HideAllPanels();
        isPaused = false;
        if (movement != null) movement.enabled = true;
        gameBegun = true;
    }

    public void HideAllPanels()
    {
        panelRoot.SetActive(false);
        panelSettings.SetActive(false);
        panelPause.SetActive(false);
    }

    public void Back()
    {
        if (panelSettings.activeSelf)
        {
            if (settingsFromPause)
            {
                ShowPause();
            }
            else
            {
                ShowRoot();
            }
            return;
        }

        if (panelPause.activeSelf)
        {
            ResumeFromPause();
        }
    }


    public IEnumerator FadeOutThenHide()
    {
        if (fader == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            SetAlpha(fader, a);
            yield return null;
        }

        HideAllPanels();
    }

    void SetAlpha(Image img, float a)
    {
        var c = img.color; c.a = a; img.color = c;
    }

    void TryPlaceInFrontOfHead()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        Vector3 gazeYaw = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (gazeYaw.sqrMagnitude < 0.0001f) gazeYaw = cam.transform.forward;

        transform.position = cam.transform.position + gazeYaw * lockedDistance + menuOffset;

        Quaternion facePlayer = Quaternion.LookRotation(-Vector3.ProjectOnPlane(transform.position - cam.transform.position, Vector3.up).normalized, Vector3.up);
        transform.rotation = facePlayer * rotationOffsetFromFacing;
    }

    private Quaternion ComputeRotationOffset()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return Quaternion.identity;

        Vector3 toHead = cam.transform.position - transform.position;
        Vector3 fwdYaw = Vector3.ProjectOnPlane(toHead, Vector3.up).normalized;
        if (fwdYaw.sqrMagnitude < 0.0001f) fwdYaw = transform.forward;

        Quaternion facePlayerAtBoot = Quaternion.LookRotation(fwdYaw, Vector3.up);

        return Quaternion.Inverse(facePlayerAtBoot) * transform.rotation;
    }


    private bool IsOnPause()
    {
        return panelPause.activeSelf && !panelSettings.activeSelf;
    }

    private bool IsOnSettings()
    {
        return panelRoot.activeSelf && panelSettings.activeSelf;
    }

    private bool IsAnyMenuVisible()
    {
        return panelRoot.activeSelf || panelSettings.activeSelf || panelPause.activeSelf;
    }

    private bool IsAnyMenuVisibleExceptPause()
    {
        return panelRoot.activeSelf && !panelPause.activeSelf;
    }

    public void NewGame()
    {
        BeginGame();
        tutorialManager.StartTutorial();
    }

    public void ContinueGame()
    {
        bool ok = SaveManager.Load("slot1");
        if (ok) ProgressManager.Instance?.ApplySpawnIfAny();
        BeginGame();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
