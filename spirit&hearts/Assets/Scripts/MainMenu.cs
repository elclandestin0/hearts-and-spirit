using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelRoot;       // container canvas for menu UIs
    public GameObject panelSettings;   // settings panel (child of root)
    public GameObject panelPause;      // pause panel (child of root)

    [Header("Fade")]
    public Image fader;
    [Range(0.1f, 3f)] public float fadeDuration = 1.0f;

    [Header("Placement")]
    public Transform xrOrigin;             // XROrigin / rig root
    public float distanceFromHead = 2f;
    public Vector3 menuOffset = new Vector3(0, -0.1f, 0);

    [Header("Controls")]
    public KeyCode pauseKey = KeyCode.Escape;

    private TutorialManager tutorialManager;
    private Movement movement;             // player movement script
    private Camera cam;
    private bool isPaused = false;
    private bool settingsFromPause = false;

    void Awake()
    {
        ShowRoot();
        if (fader != null) SetAlpha(fader, 0f);
    }

    void Start()
    {
        cam = Camera.main;
        TryPlaceInFrontOfHead();

        var player = GameObject.FindGameObjectWithTag("MainPlayer");
        tutorialManager = player.GetComponent<TutorialManager>();
        movement = player.GetComponent<Movement>();

        // Boot behavior: show main menu (not paused)
        panelRoot.SetActive(true);
        panelPause.SetActive(false);
        panelSettings.SetActive(false);
        isPaused = false;
        if (movement != null) movement.enabled = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (!isPaused)
            {
                PauseAndShowPause();
            }
            else
            {
                if (IsOnPause())
                    ResumeFromPause();  // pause -> pause again -> unpause
                else
                    ShowPause();        // paused but on Settings -> go back to Pause
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


    private void PauseAndShowPause()
    {
        isPaused = true;

        if (movement != null) movement.enabled = false;

        TryPlaceInFrontOfHead();
        ShowPause();
    }

    private void ResumeFromPause()
    {
        isPaused = false;

        HideAllPanels();
        if (movement != null) movement.enabled = true;
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
        // Enter gameplay from main menu
        HideAllPanels();
        isPaused = false;
        if (movement != null) movement.enabled = true;
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
        if (xrOrigin == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;

        transform.position = cam.transform.position + forward * distanceFromHead + menuOffset;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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
