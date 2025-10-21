using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleController : MonoBehaviour
{
    [Header("Refs")]
    public TutorialManager tutorialManager;   // optional
    public DovinaAudioManager audioMgr;
    public TMP_Text subtitleUI;
    public TMP_Text subtitleToggle;
    public TMP_Text subtitleTogglePause;
    public SubtitleDatabase database;

    [Header("Behavior")]
    public float extraHoldAfterEnd = 1f;
    public bool clearWhenInterrupted = true;

    Coroutine hideRoutine;
    Coroutine bindRoutine;
    int currentToken = -1;
    bool bound = false;

    void Awake()
    {
        subtitleToggle.text = subtitleUI.enabled ? "X" : "";
        subtitleTogglePause.text = subtitleUI.enabled ? "X" : "";
    }

    void OnEnable()
    {
        // Don't clear here—tutorial may be showing a line right now.
        if (bindRoutine != null) StopCoroutine(bindRoutine);
        bindRoutine = StartCoroutine(BindAfterTutorial());
    }

    void OnDisable()
    {
        Unbind();
        if (bindRoutine != null) { StopCoroutine(bindRoutine); bindRoutine = null; }
    }

    IEnumerator BindAfterTutorial()
    {
        // If there's a tutorial, wait until it's done.
        while (tutorialManager != null && tutorialManager.IsRunning)
            yield return null;

        Bind();

        // Now we own the subtitle UI; optionally clear once we’re in control.
        if (subtitleUI && string.IsNullOrEmpty(subtitleUI.text))
            subtitleUI.text = "";
    }

    void Bind()
    {
        if (audioMgr != null && !bound)
        {
            audioMgr.OnClipStarted += HandleStarted;
            audioMgr.OnClipEnded += HandleEnded;
            audioMgr.OnClipInterrupted += HandleInterrupted;
            bound = true;
        }
    }

    void Unbind()
    {
        if (audioMgr != null && bound)
        {
            audioMgr.OnClipStarted -= HandleStarted;
            audioMgr.OnClipEnded -= HandleEnded;
            audioMgr.OnClipInterrupted -= HandleInterrupted;
            bound = false;
        }
    }

    // --- Events from DovinaAudioManager ---
    void HandleStarted(AudioClip clip, int token)
    {
        // Safety: if tutorial somehow still running, ignore.
        if (tutorialManager != null && tutorialManager.IsRunning) return;

        currentToken = token;
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }

        if (database != null && database.TryGet(clip, out var text) && !string.IsNullOrEmpty(text))
        {
            if (subtitleUI) { subtitleUI.gameObject.SetActive(true); subtitleUI.text = text; }
        }
        else
        {
            if (subtitleUI) subtitleUI.text = "";
        }
    }

    void HandleEnded(int token)
    {
        if (token != currentToken) return;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideSoon());
    }

    void HandleInterrupted(int token)
    {
        if (!clearWhenInterrupted) return;
        if (token != currentToken) return;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        HideNow();
    }

    IEnumerator HideSoon()
    {
        yield return new WaitForSeconds(extraHoldAfterEnd);
        HideNow();
    }

    void HideNow()
    {
        if (subtitleUI) subtitleUI.text = "";
        hideRoutine = null;
    }

    public void ToggleSubtitles()
    {
        subtitleUI.enabled = !subtitleUI.enabled;
        subtitleToggle.text = subtitleUI.enabled ? "X" : "";
        subtitleTogglePause.text = subtitleUI.enabled ? "X" : "";
    }
}
