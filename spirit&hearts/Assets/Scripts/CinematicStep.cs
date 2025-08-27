using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(menuName = "Tutorial/Cinematic Step")]
public class CinematicStep : ScriptableObject
{
    public string displayName = "Intro";
    public MovementAbility allowedAbilities = MovementAbility.Look; // lock movement
    public List<CineAction> actions = new(); // executed in order
}

// Context passed to actions
public class CineContext
{
    public Transform playerHead;
    public Transform playerRoot;
    public Transform arrivalPoint;
    public DoveCompanion dove;
    public DovinaAudioManager speaker;
    public System.Func<string, Transform> ResolveTarget; // optional ID → Transform
}
