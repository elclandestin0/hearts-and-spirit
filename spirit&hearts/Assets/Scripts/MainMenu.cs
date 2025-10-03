using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelRoot;
    public GameObject panelPlay;
    public GameObject panelSettings;

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

    void Awake()
    {
        ShowRoot();
        if (fader != null) SetAlpha(fader, 0f);
    }

    void Start()
    {
        cam = Camera.main;
        TryPlaceInFrontOfHead();
        tutorialManager = GameObject.FindGameObjectWithTag("MainPlayer").GetComponent<TutorialManager>();
        movement = GameObject.FindGameObjectWithTag("MainPlayer").GetComponent<Movement>();

        // start hidden until explicitly shown
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) ResumeFromPause();
            else PauseAndShowMenu();
        }
    }


    private void PauseAndShowMenu()
    {
        isPaused = true;

        // Freeze player movement by disabling the Movement script.
        // (This guarantees no forces/glide/gravity are applied while paused.)
        if (movement != null) movement.enabled = false;

        // Re-position the menu in front of wherever the player is looking *right now*.
        TryPlaceInFrontOfHead();

        // Show root panel
        ShowRoot();
        panelRoot.SetActive(true);
    }

    private void ResumeFromPause()
    {
        isPaused = false;

        // Hide menu panels
        panelRoot.SetActive(false);
        panelPlay.SetActive(false);
        panelSettings.SetActive(false);

        // Re-enable movement
        if (movement != null) movement.enabled = true;
    }

    // ————— Placement —————

    void TryPlaceInFrontOfHead()
    {
        if (xrOrigin == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // keep it on the horizontal plane, so it doesn’t tilt up/down with the head
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;

        transform.position = cam.transform.position + forward * distanceFromHead + menuOffset;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // ————— Panel API (unchanged) —————
    public void ShowRoot()
    {
        panelRoot.SetActive(true);
        panelPlay.SetActive(false);
        panelSettings.SetActive(false);
    }

    public void ShowPlay()
    {
        panelRoot.SetActive(false);
        panelPlay.SetActive(true);
        panelSettings.SetActive(false);
    }

    public void ShowSettings()
    {
        panelRoot.SetActive(false);
        panelPlay.SetActive(false);
        panelSettings.SetActive(true);
    }

    public void BeginGame()
    {
        panelRoot.SetActive(false);
        panelPlay.SetActive(false);
        panelSettings.SetActive(false);
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

        panelRoot.SetActive(false);
        panelPlay.SetActive(false);
        panelSettings.SetActive(false);
    }

    void SetAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }  

    // ————— Buttons —————

    public void NewGame()
    {
        BeginGame();
        // closing pause if it was open
        if (isPaused) ResumeFromPause();

        tutorialManager.StartTutorial();
    }

    public void ContinueGame()
    {
        bool ok = SaveManager.Load("slot1");
        if (ok)
        {
            ProgressManager.Instance?.ApplySpawnIfAny();
        }

        BeginGame();
        if (isPaused) ResumeFromPause();
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
