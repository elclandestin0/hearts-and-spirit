using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MovementEventHub))]
public class TutorialManager : MonoBehaviour, IMovementPolicyProvider
{
    [Header("Steps (CinematicStep and TutorialStep assets)")]
    [SerializeField] private ScriptableObject[] steps;

    [Header("Scene Refs")]
    [SerializeField] private DovinaAudioManager doveSpeaker;   // optional; not required for compile
    [SerializeField] private DoveCompanion doveController;     // your dove mover/brain
    [SerializeField] private Transform doveArrivalPoint;       // empty near player head height
    [SerializeField] private Transform playerHead;
    [SerializeField] private bool autoStart = true;

    // Progress
    public int StepIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }

    // Policy served to Movement.cs
    private MovementPolicy _policy;
    public MovementPolicy CurrentPolicy => _policy;

    // Events & counters for interactive steps
    private MovementEventHub _events;
    private int   flapCount;
    private float glideSec, diveSec, hoverSec;
    private float stepClock;

    // Current step handles
    private TutorialStep  currentInteractive = null;
    private CinematicStep currentCinematic   = null;
    private Coroutine     cinematicRoutine   = null;

    void Awake()
    {
        _events = GetComponent<MovementEventHub>();
        // counters for interactive completion
        _events.OnFlap.AddListener(() => flapCount++);
        _events.OnGlideTick.AddListener(dt => glideSec += dt);
        _events.OnDiveTick.AddListener(dt  => diveSec  += dt);
        _events.OnHoverTick.AddListener(dt => hoverSec += dt);
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
        currentCinematic   = null;

        StepIndex++;
        if (steps == null || StepIndex >= steps.Length)
        {
            EndTutorialToFreeFlight();
            return;
        }

        var so = steps[StepIndex];

        // Try cinematic first
        currentCinematic = so as CinematicStep;
        if (currentCinematic != null)
        {
            // lock to cinematic policy (usually Look only)
            _policy = new MovementPolicy { Allowed = currentCinematic.allowedAbilities };

            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            cinematicRoutine = StartCoroutine(RunCinematic(currentCinematic));
            return;
        }

        // Then interactive
        currentInteractive = so as TutorialStep;
        if (currentInteractive != null)
        {
            _policy = new MovementPolicy { Allowed = currentInteractive.allowedAbilities };

            // reset counters & clock for this interactive step
            flapCount = 0;
            glideSec = diveSec = hoverSec = 0f;
            stepClock = 0f;

            // (Optional) present text/VO here using your own system, but kept out for compile-safety.
            // Example (only if your DovinaAudioManager has a safe API):
            // doveSpeaker?.PlayLine(currentInteractive.subtitleText, currentInteractive.doveVO);

            return;
        }

        // Unsupported asset type—skip forward safely.
        Debug.LogWarning($"TutorialManager: Unsupported step type at index {StepIndex}: {so}");
        Advance();
    }

    private IEnumerator RunCinematic(CinematicStep cine)
    {
        // Build context for actions (requires your CineContext class)
        var ctx = new CineContext
        {
            playerHead = playerHead,
            playerRoot = transform,
            dove       = doveController,
            speaker    = doveSpeaker
        };

        // Inject scene refs that can't live in assets (e.g., arrival point)
        if (cine.actions != null)
        {
            foreach (var act in cine.actions)
            {
                if (act is CineMoveDoveTo move && move.target == null)
                    move.target = doveArrivalPoint;
            }

            // Execute actions in order
            foreach (var act in cine.actions)
            {
                if (act != null)
                    yield return StartCoroutine(act.Execute(ctx));
            }
        }

        cinematicRoutine = null;
        Advance(); // continue to next step (likely the first interactive one)
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
                if (flapCount >= currentInteractive.targetFlapCount) Advance();
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
        }
    }
}
