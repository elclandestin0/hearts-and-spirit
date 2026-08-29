using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space session UI for the flatscreen build, created entirely from code
/// (the scene's canvases are world-space VR menus). Text is TextMeshPro: the TMP
/// font asset lives in Assets/TextMesh Pro/Resources, so unlike the built-in
/// legacy font it can never be stripped out of a player build.
///
/// Closed panel:  [ FLY TOGETHER ] button + controls hint.
/// Open panel:    HOST A FLIGHT / join-code box + JOIN.
/// In session:    the shareable code, player count, LEAVE.
/// </summary>
public class SessionHud : MonoBehaviour
{
    public static SessionHud Instance { get; private set; }

    TMP_Text _status, _codeText, _playersText, _toast;
    TMP_InputField _codeInput;
    GameObject _menuPanel, _sessionPanel, _openButton;
    float _toastUntil;
    bool _busy;

    public static void Ensure()
    {
        if (Instance != null) return;
        var go = new GameObject("SessionHud");
        DontDestroyOnLoad(go);
        go.AddComponent<SessionHud>();
    }

    public void Notify(string message)
    {
        if (_toast == null) return;
        _toast.text = message;
        _toastUntil = Time.unscaledTime + 4f;
    }

    void Awake()
    {
        Instance = this;
        BuildUi();
        NetSession.Changed += Refresh;
        Refresh();
        Debug.Log("[SessionHud] UI built.");
    }

    void OnDestroy()
    {
        NetSession.Changed -= Refresh;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        DebugKeys.SuppressForTyping = _codeInput != null && _codeInput.isFocused;

        if (_toast != null && _toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
            _toast.text = "";

        if (_playersText != null && NetSession.InSession)
            _playersText.text = $"birds in the sky: {Mathf.Max(1, NetAvatar.All.Count)}";
    }

    void Refresh()
    {
        bool inSession = NetSession.InSession;
        _menuPanel.SetActive(false);
        _sessionPanel.SetActive(inSession);
        _openButton.SetActive(!inSession);
        if (inSession)
            _codeText.text = NetSession.JoinCode;
        _busy = false;
        SetStatus("");
    }

    void SetStatus(string s) { if (_status != null) _status.text = s; }

    // ---- Actions --------------------------------------------------------

    async void OnHost()
    {
        if (_busy) return;
        _busy = true;
        SetStatus("Opening the sky…");
        try
        {
            string code = await NetSession.HostAsync();
            Notify($"Session up — share code {code}");
        }
        catch (Exception e)
        {
            SetStatus("Couldn't host: " + Short(e));
            _busy = false;
        }
    }

    async void OnJoin()
    {
        if (_busy) return;
        string code = _codeInput.text.Trim();
        if (code.Length < 4) { SetStatus("Type the 6-letter code first"); return; }
        _busy = true;
        SetStatus("Joining…");
        try
        {
            await NetSession.JoinAsync(code);
            Notify("Joined! Fly to your friend");
        }
        catch (Exception e)
        {
            SetStatus("Couldn't join: " + Short(e));
            _busy = false;
        }
    }

    async void OnLeave()
    {
        if (_busy) return;
        _busy = true;
        try { await NetSession.LeaveAsync(); }
        catch (Exception e) { Debug.LogWarning(e); }
        _busy = false;
        Refresh();
    }

    static string Short(Exception e)
    {
        var msg = e.InnerException?.Message ?? e.Message;
        return msg.Length > 90 ? msg.Substring(0, 90) + "…" : msg;
    }

    // ---- UI construction -------------------------------------------------

    static readonly Color PanelBg = new Color(0.07f, 0.08f, 0.12f, 0.92f);
    static readonly Color BtnBg = new Color(0.16f, 0.2f, 0.3f, 1f);
    static readonly Color BtnAccent = new Color(0.95f, 0.78f, 0.3f, 1f);
    static readonly Color TextCol = new Color(0.95f, 0.95f, 0.9f, 1f);

    void BuildUi()
    {
        var canvasGO = new GameObject("SessionCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Controls hint — always visible, bottom center.
        var hint = MakeText(canvasGO.transform, "hint",
            "F flap (hold to charge)   ·   M glide   ·   N hover   ·   W/S/A/D + hold right-mouse: look   ·   X shoot seed   ·   C call seed   ·   Esc pause",
            13, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(1400, 24));
        hint.color = new Color(1f, 1f, 1f, 0.55f);

        // Toast — top center.
        _toast = MakeText(canvasGO.transform, "toast", "", 20, TextAlignmentOptions.Center);
        Place(_toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(900, 32));
        _toast.color = BtnAccent;

        // Open button — top-left.
        _openButton = MakeButton(canvasGO.transform, "open", "FLY TOGETHER", BtnAccent, () =>
        {
            _menuPanel.SetActive(!_menuPanel.activeSelf);
        }, textColor: Color.black);
        Place(_openButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(110, -34), new Vector2(180, 42));

        // --- Menu panel (host/join) ---
        _menuPanel = MakePanel(canvasGO.transform, "menu");
        Place(_menuPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(190, -190), new Vector2(340, 240));

        var title = MakeText(_menuPanel.transform, "title", "FLY WITH A FRIEND", 18, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(320, 26));

        var hostBtn = MakeButton(_menuPanel.transform, "host", "HOST A FLIGHT", BtnAccent, OnHost, textColor: Color.black);
        Place(hostBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(280, 44));

        _codeInput = MakeInput(_menuPanel.transform, "code e.g. ABC123");
        Place(_codeInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(-45, -128), new Vector2(180, 40));

        var joinBtn = MakeButton(_menuPanel.transform, "join", "JOIN", BtnBg, OnJoin);
        Place(joinBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(95, -128), new Vector2(90, 40));

        _status = MakeText(_menuPanel.transform, "status", "", 13, TextAlignmentOptions.Top);
        Place(_status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(320, 60));
        _status.color = new Color(1f, 0.8f, 0.7f);

        // --- In-session panel ---
        _sessionPanel = MakePanel(canvasGO.transform, "session");
        Place(_sessionPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(180, -110), new Vector2(320, 150));

        var codeLabel = MakeText(_sessionPanel.transform, "codeLabel", "SHARE THIS CODE", 12, TextAlignmentOptions.Center);
        Place(codeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(300, 18));

        _codeText = MakeText(_sessionPanel.transform, "code", "------", 34, TextAlignmentOptions.Center);
        Place(_codeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(300, 40));
        _codeText.color = BtnAccent;
        _codeText.fontStyle = FontStyles.Bold;

        _playersText = MakeText(_sessionPanel.transform, "players", "birds in the sky: 1", 14, TextAlignmentOptions.Center);
        Place(_playersText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(300, 20));

        var leaveBtn = MakeButton(_sessionPanel.transform, "leave", "LEAVE", new Color(0.45f, 0.16f, 0.16f), OnLeave);
        Place(leaveBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(120, 34));

        _menuPanel.SetActive(false);
        _sessionPanel.SetActive(false);
    }

    GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = PanelBg;
        return go;
    }

    TMP_Text MakeText(Transform parent, string name, string content, int size, TextAlignmentOptions anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        // font left null on purpose: TMP falls back to TMP Settings' default font
        // asset, which ships in Assets/TextMesh Pro/Resources.
        t.text = content;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = TextCol;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    GameObject MakeButton(Transform parent, string name, string label, Color bg, Action onClick, Color? textColor = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        var t = MakeText(go.transform, "label", label, 16, TextAlignmentOptions.Center);
        t.color = textColor ?? TextCol;
        t.fontStyle = FontStyles.Bold;
        Fill(t.rectTransform);
        return go;
    }

    TMP_InputField MakeInput(Transform parent, string placeholder)
    {
        var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.transform.SetParent(parent, false);
        var input = go.GetComponent<TMP_InputField>();
        input.characterLimit = 8;
        if (input.placeholder is TMP_Text ph)
        {
            ph.text = placeholder;
            ph.fontSize = 14;
        }
        if (input.textComponent != null)
            input.textComponent.fontSize = 18;
        return input;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Fill(RectTransform rt, float pad = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, 0);
        rt.offsetMax = new Vector2(-pad, 0);
    }
}
