// TutorialStep.cs
using UnityEngine;

public enum TutorialCompletionType
{
    None,
    FlapCount,         // e.g., perform N flaps
    GlideDuration,     // glide for >= seconds
    DiveDuration,      // dive for >= seconds
    HoverDuration,      // hover for >= seconds (idle airborne)
    FallDuration,       // How long the player needs to fall for before completing a step 
}

[CreateAssetMenu(menuName="Tutorial/Step")]
public class TutorialStep : ScriptableObject
{
    [Header("What is allowed in this step")]
    public MovementAbility allowedAbilities = MovementAbility.Look;

    [Header("What finishes this step")]
    public TutorialCompletionType completionType = TutorialCompletionType.None;
    public int targetFlapCount = 0;
    public float targetSeconds   = 0f;

    [Header("Presentation")]
    public string subtitleText;
    public AudioClip doveVO; // optional
    public float minSecondsBeforeAdvance = 0.5f; // avoid instant skip
}
