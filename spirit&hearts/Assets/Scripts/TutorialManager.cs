using UnityEngine;

[RequireComponent(typeof(MovementEventHub))]
public class TutorialManager : MonoBehaviour, IMovementPolicyProvider
{
    [SerializeField] private TutorialStep[] steps;   // assign in Inspector
    [SerializeField] private DovinaAudioManager dove;       // optional VO/subtitles bridge
    [SerializeField] private bool autoStart = true;

    // Progress (expose for UI if needed)
    public int StepIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }

    // Policy served to Movement.cs
    private MovementPolicy _policy;
    public MovementPolicy CurrentPolicy => _policy;

    // Internals
    private MovementEventHub _eventHub;
    private int flapCount;
    private float glideSec, diveSec, hoverSec;
    private float stepClock;

    void Awake()
    {
        _eventHub = GetComponent<MovementEventHub>();
        _eventHub.OnFlap.AddListener(() => flapCount++);
        _eventHub.OnGlideTick.AddListener(dt => glideSec += dt);
        _eventHub.OnDiveTick.AddListener(dt  => diveSec  += dt);
        _eventHub.OnHoverTick.AddListener(dt => hoverSec += dt);
    }

    void Start()
    {
        if (autoStart)
        {
            // Optional: skip tutorial if already completed in a prior session
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        IsRunning = true;
        StepIndex = -1;
        Advance();
    }

    void Advance()
    {
        StepIndex++;
        if (StepIndex >= steps.Length) { EndTutorialToFreeFlight(); return; }

        var s = steps[StepIndex];
        _policy = new MovementPolicy { Allowed = s.allowedAbilities};

        // reset counters for this step
        flapCount = 0; glideSec = diveSec = hoverSec = 0f; stepClock = 0f;
    }

    void EndTutorialToFreeFlight()
    {
        // Grant everything
        _policy = new MovementPolicy {
            Allowed = MovementAbility.Look | MovementAbility.Translate | MovementAbility.Flap | MovementAbility.Glide | MovementAbility.Dive | MovementAbility.Hover
        };
        IsRunning = false;

        enabled = false; // manager no longer needs to tick
    }

    void Update()
    {
        if (!IsRunning) return;
        stepClock += Time.deltaTime;

        var s = steps[StepIndex];
        if (stepClock < s.minSecondsBeforeAdvance) return;

        // Optional: auto-advance when VO finishes for “None” steps
        // bool voDone = !dove || dove.IsLineFinished;

        switch (s.completionType)
        {
            case TutorialCompletionType.None:
                // if (voDone) Advance();
                break;

            case TutorialCompletionType.FlapCount:
                if (flapCount >= s.targetFlapCount) Advance();
                break;

            case TutorialCompletionType.GlideDuration:
                if (glideSec >= s.targetSeconds) Advance();
                break;

            case TutorialCompletionType.DiveDuration:
                if (diveSec >= s.targetSeconds) Advance();
                break;

            case TutorialCompletionType.HoverDuration:
                if (hoverSec >= s.targetSeconds) Advance();
                break;
        }
    }
}
