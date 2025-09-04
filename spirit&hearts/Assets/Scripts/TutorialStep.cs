// TutorialStep.cs
using UnityEngine;

public enum TutorialCompletionType
{
    None,
    FlapCount,
    GlideDuration,
    DiveDuration,
    HoverDuration,
    FallDuration,
    LookDuration,
    GravityDuration,
    Nodding,
    SeedPicked,
    LightLit
}

[CreateAssetMenu(menuName="Tutorial/Step")]
public class TutorialStep : ScriptableObject
{
    [Header("What is allowed in this step")]
    public MovementAbility allowedAbilities = MovementAbility.Look;

    [Header("Physics")]
    public bool gravityEnabled = true;

    [Header("What finishes this step")]
    public TutorialCompletionType completionType = TutorialCompletionType.None;
    public int targetCount = 0;
    public float targetSeconds = 0f;

    [Header("Presentation")]
    [TextArea] public string subtitleText;
    public AudioClip doveVO; // optional
    public float minSecondsBeforeAdvance = 0.5f; // avoid instant skip
}