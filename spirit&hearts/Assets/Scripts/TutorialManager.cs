using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(MovementEventHub))]
public class TutorialManager : MonoBehaviour, IMovementPolicyProvider
{
    [Header("Steps (CinematicStep and TutorialStep assets)")]
    [SerializeField] private ScriptableObject[] steps;
    [Header("Scene Refs")]
    [SerializeField] private DovinaAudioManager doveSpeaker;   // optional; not required for compile#
    [SerializeField] private DoveCompanion doveController;     // your dove mover/brain
    [SerializeField] private Transform doveArrivalPoint;       // empty near player head height
    [SerializeField] private Transform playerHead;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private TMP_Text subtitleUI;
    private int seedsPicked, lightsLit;
    // Progress
    public int StepIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }

    // Policy served to Movement.cs
    private MovementPolicy _policy;
    public MovementPolicy CurrentPolicy => _policy;

    // Events & counters for interactive steps
    private MovementEventHub _events;
    private int flapCount, nodCount;
    private float glideSec, diveSec, hoverSec, lookSec, gravitySec;
    private float stepClock;

    // Headset escape hatch: hold both controller face buttons to skip.
    // The keyboard shortcut below is useless to anyone actually wearing the
    // headset, which is a problem if a step fails to complete.
    [Header("Skip Gesture")]
    [SerializeField] private float skipHoldSeconds = 2f;
    private readonly List<InputDevice> _xrControllers = new List<InputDevice>();
    private float _skipHeldFor;

    // Current step handles
    private TutorialStep currentInteractive = null;
    private CinematicStep currentCinematic = null;
    private Coroutine cinematicRoutine = null;

    void Awake()
    {
        _events = GetComponent<MovementEventHub>();

        // counters for interactive completion
        _events.OnLookTick.AddListener(dt => lookSec += dt);
        _events.OnFlap.AddListener(() => flapCount++);
        _events.OnGlideTick.AddListener(dt => glideSec += dt);
        _events.OnDiveTick.AddListener(dt => diveSec += dt);
        _events.OnHoverTick.AddListener(dt => hoverSec += dt);
        _events.OnNod.AddListener(() => nodCount++);
        _events.OnGravityTick.AddListener(dt => gravitySec += dt);
        _events.OnSeedPicked.AddListener(() => seedsPicked++);
        _events.OnLightLit.AddListener(() => lightsLit++);
    }

    void Start()
    {
        if (autoStart) StartTutorial();
        IsRunning = true;
    }

    public void StartTutorial()
    {
        if (PauseState.IsPaused) return;
        // doveController.StartInfiniteFlap();
        IsRunning = true;
        StepIndex = -1;
        Advance();
    }

    public void SkipTutorial()
    {
        // If a cinematic is in progress, stop it safely.
        if (cinematicRoutine != null)
        {
            StopCoroutine(cinematicRoutine);
            cinematicRoutine = null;
            GameLog.Log("Stopping cinematic routine...");
        }

        GameLog.Log("End of Skip Tutorial...");
        EndTutorialToFreeFlight();
    }

    private void Advance()
    {
        if (PauseState.IsPaused) return;
        currentInteractive = null;
        currentCinematic = null;

        StepIndex++;
        if (steps == null || StepIndex >= steps.Length)
        {
            GameLog.Log("Invoking EndTutorialToFreeFlight()");
            EndTutorialToFreeFlight();
            return;
        }

        var so = steps[StepIndex];
        currentCinematic = so as CinematicStep;
        if (currentCinematic != null)
        {
            // lock to cinematic policy (usually Look only).
            // Cinematics always hold the player still - CinematicStep has no
            // gravity setting of its own. This used to read currentInteractive,
            // which Advance() nulls a few lines up, so it only ever meant false.
            _policy = new MovementPolicy
            {
                Allowed = currentCinematic.allowedAbilities,
                GravityEnabled = false
            };

            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            cinematicRoutine = StartCoroutine(RunCinematic(currentCinematic));
            return;
        }

        // Then interactive
        currentInteractive = so as TutorialStep;
        if (currentInteractive != null)
        {
            _policy = new MovementPolicy
            {
                Allowed = currentInteractive.allowedAbilities,
                GravityEnabled = currentInteractive.gravityEnabled
            };

            // reset step-local counters
            flapCount = 0;
            glideSec = diveSec = hoverSec = lookSec = gravitySec = 0f;
            nodCount = 0;

            // reset NEW counters for this step
            seedsPicked = 0;
            lightsLit = 0;

            stepClock = 0f;

            var movementScript = GetComponent<Movement>();
            movementScript.isGliding = false;
            movementScript.isHovering = false;

            doveSpeaker?.PlayClip(currentInteractive.doveVO, 2);
            if (!string.IsNullOrEmpty(currentInteractive.subtitleText) && subtitleUI != null)
                subtitleUI.text = currentInteractive.subtitleText;

            return;
        }

        Debug.LogWarning($"TutorialManager: Unsupported step type at index {StepIndex}: {so}");
        Advance();
    }

    [System.Serializable]
    public struct NamedPoint { public string id; public Transform transform; }

    [SerializeField] private NamedPoint[] scenePoints; // fill in inspector
    private Transform ResolvePoint(string id)
    {
        foreach (var p in scenePoints)
        {
            GameLog.Log($"[ResolvePoint] Comparing '{p.id}' with '{id}'");
            if (p.id == id)
            {
                GameLog.Log($"[ResolvePoint] MATCH found for {id}, transform={p.transform.name}");
                return p.transform;
            }
        }
        Debug.LogWarning($"[ResolvePoint] No match for '{id}'");
        return null;
    }
    private IEnumerator RunCinematic(CinematicStep cine)
    {
        if (PauseState.IsPaused)
            yield return new WaitUntil(() => !PauseState.IsPaused);

        var ctx = new CineContext
        {
            playerHead = playerHead,
            playerRoot = transform,
            arrivalPoint = doveArrivalPoint,
            dove = doveController,
            speaker = doveSpeaker,
            ResolveTarget = ResolvePoint,
            subtitleUI = subtitleUI,
            runner = this
        };

        if (cine.actions != null)
        {
            foreach (var act in cine.actions)
            {
                if (act == null) continue;

                // NEW: if paused, wait here (prevents advancing to the next action while paused)
                if (PauseState.IsPaused)
                    yield return PauseYields.UntilUnpaused();

                var runtimeAct = ScriptableObject.Instantiate(act);
                yield return StartCoroutine(runtimeAct.Execute(ctx));
                Destroy(runtimeAct);
            }
        }

        cinematicRoutine = null;
        Advance();
    }



    private void EndTutorialToFreeFlight()
    {
        // Grant everything
        _policy = MovementPolicy.Everything;

        // Optional: clear any on-screen subtitle
        if (subtitleUI != null) subtitleUI.text = "";

        // Mark finished and stop ticking
        IsRunning = false;
        enabled = false;

        GameLog.Log("[Tutorial] Skipped → Free Flight (Everything enabled).");
    }

    /// <summary>True while both hand controllers report their primary face button down.</summary>
    private bool HoldingSkipGesture()
    {
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            _xrControllers);

        int pressed = 0;
        foreach (var device in _xrControllers)
        {
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool down) && down)
                pressed++;
        }
        return pressed >= 2;
    }

    void Update()
    {
        if (DebugKeys.Pressed(KeyCode.P))
        {
            GameLog.Log("Skipping tutorial...");
            SkipTutorial();
            return;
        }

        if (PauseState.IsPaused)
        {
            return;
        }

        if (IsRunning && HoldingSkipGesture())
        {
            _skipHeldFor += Time.unscaledDeltaTime;
            if (_skipHeldFor >= skipHoldSeconds)
            {
                GameLog.Log("[Tutorial] Skip gesture held; skipping tutorial.");
                _skipHeldFor = 0f;
                SkipTutorial();
                return;
            }
        }
        else
        {
            _skipHeldFor = 0f;
        }

        if (!IsRunning || currentInteractive == null) return;

        stepClock += Time.deltaTime;

        // Guard: ensure we don't read before min delay
        if (stepClock < currentInteractive.minSecondsBeforeAdvance) return;

        switch (currentInteractive.completionType)
        {
            case TutorialCompletionType.None:
                // Auto-advance once minSecondsBeforeAdvance has passed.
                Advance();
                break;

            case TutorialCompletionType.FlapCount:
                if (flapCount >= currentInteractive.targetCount) Advance();
                break;

            case TutorialCompletionType.GlideDuration:
                if (glideSec >= currentInteractive.targetSeconds) Advance();
                break;

            case TutorialCompletionType.DiveDuration:
                if (diveSec >= currentInteractive.targetSeconds) Advance();
                break;

            case TutorialCompletionType.HoverDuration:
                if (hoverSec >= currentInteractive.targetSeconds) Advance();
                break;

            case TutorialCompletionType.Nodding:
                if (nodCount >= currentInteractive.targetCount) Advance();
                break;

            case TutorialCompletionType.LookDuration:
                if (lookSec >= currentInteractive.targetSeconds) Advance();
                break;
            case TutorialCompletionType.GravityDuration:
                if (gravitySec >= currentInteractive.targetSeconds) Advance();
                break;

            case TutorialCompletionType.SeedPicked:
                if (seedsPicked >= Mathf.Max(1, currentInteractive.targetCount)) Advance();
                break;

            case TutorialCompletionType.LightLit:
                if (lightsLit >= Mathf.Max(1, currentInteractive.targetCount)) Advance();
                break;
        }
    }
}
