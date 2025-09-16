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
    public Transform xrOrigin;     // Assign your XROrigin
    public float distanceFromHead = 2f;
    public Vector3 menuOffset = new Vector3(0, -0.1f, 0); // slight vertical offset
    private TutorialManager tutorialManager;

    void Awake()
    {
        ShowRoot();
        if (fader != null) SetAlpha(fader, 0f);
    }

    void Start()
    {
        // Place in front of HMD once at boot
        TryPlaceInFrontOfHead();
        tutorialManager = GameObject.FindGameObjectWithTag("MainPlayer").GetComponent<TutorialManager>();
    }

    void TryPlaceInFrontOfHead()
    {
        if (xrOrigin == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;

        transform.position = cam.transform.position + forward * distanceFromHead + menuOffset;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    #region Panel API
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
    #endregion

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

        // Hide all panels after fade to black
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

    public void NewGame()
    {
        BeginGame();
        tutorialManager.StartTutorial();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the application in a build
        Application.Quit();
#endif
    }

    public void ContinueGame()
    {
        bool ok = SaveManager.Load("slot1");
        if (ok)
        {
            ProgressManager.Instance?.ApplySpawnIfAny();
        }

        BeginGame();
    }
}
