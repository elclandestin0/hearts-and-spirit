using UnityEngine;
using System.Collections;
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
    }

    void Start()
    {
        if (autoStart) StartTutorial();
    }

    public void StartTutorial()
    {
        IsRunning = true;
        StepIndex = -1;
        Advance();
    }

    private void Advance()
    {
        // clear current step refs
        currentInteractive = null;
        currentCinematic = null;

        StepIndex++;
        if (steps == null || StepIndex >= steps.Length)
        {
            EndTutorialToFreeFlight();
            return;
        }

        var so = steps[StepIndex];
        Debug.Log(so);

        // Try cinematic first
        currentCinematic = so as CinematicStep;
        if (currentCinematic != null)
        {
            // lock to cinematic policy (usually Look only)
            _policy = new MovementPolicy
            {
                Allowed = currentCinematic.allowedAbilities,
                GravityEnabled = currentInteractive ? currentInteractive.gravityEnabled : false
            };

            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            cinematicRoutine = StartCoroutine(RunCinematic(currentCinematic));
            return;
        }

        // Then interactive
        currentInteractive = so as TutorialStep;
        if (currentInteractive != null || currentCinematic != null)
        {
            if (currentInteractive != null)
                _policy = new MovementPolicy
                {
                    Allowed = currentInteractive.allowedAbilities,
                    GravityEnabled = currentInteractive.gravityEnabled
                };

            // reset counters & clock for this interactive step
            flapCount = 0;
            glideSec = diveSec = hoverSec = lookSec = gravitySec = 0f;
            nodCount = 0;
            stepClock = 0f;

            doveSpeaker?.PlayClip(currentInteractive.doveVO, 2);
            return;
        }

        // Unsupported asset type—skip forward safely.
        Debug.LogWarning($"TutorialManager: Unsupported step type at index {StepIndex}: {so}");
        Advance();
    }

    [System.Serializable]
    public struct NamedPoint { public string id; public Transform transform; }

    [SerializeField] private NamedPoint[] scenePoints; // fill in inspector

    private Transform ResolvePoint(string id)
    {
        for (int i = 0; i < scenePoints.Length; i++)
            if (scenePoints[i].id == id) return scenePoints[i].transform;
        return null;
    }

    private IEnumerator RunCinematic(CinematicStep cine)
    {
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

                // Clone the asset so we mutate nothing on disk (safe for builds)
                var runtimeAct = ScriptableObject.Instantiate(act);

                // No target assignment here — CineMoveDoveTo resolves via ctx
                yield return StartCoroutine(runtimeAct.Execute(ctx));

                // Optional cleanup
                Destroy(runtimeAct);
            }
        }

        cinematicRoutine = null;
        Advance();
    }


    private void EndTutorialToFreeFlight()
    {
        _policy = new MovementPolicy
        {
            Allowed = MovementAbility.Look | MovementAbility.Translate |
                      MovementAbility.Flap | MovementAbility.Glide |
                      MovementAbility.Dive | MovementAbility.Hover
        };

        IsRunning = false;
        enabled = false; // manager no longer needs to tick
    }

    void Update()
    {
        if (!IsRunning || currentInteractive == null) return;

        stepClock += Time.deltaTime;

        // Guard: ensure we don't read before min delay
        if (stepClock < currentInteractive.minSecondsBeforeAdvance) return;

        // If you later expose "VO finished" from doveSpeaker, you can add it in the None case.

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
        }
    }
}
