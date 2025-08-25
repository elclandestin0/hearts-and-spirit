// MovementPolicy.cs
using System;

[Flags]
public enum MovementAbility
{
    None  = 0,
    Look  = 1 << 0,  // head/camera look
    Flap  = 1 << 1,
    Glide = 1 << 2,
    Dive  = 1 << 3,
    Hover = 1 << 4,  // maintain/feather
    Translate = 1 << 5 // any positional movement at all
}

public struct MovementPolicy
{
    public MovementAbility Allowed; // bitmask
    public static MovementPolicy LockedLookOnly => new MovementPolicy {
        Allowed = MovementAbility.Look
    };
}

public interface IMovementPolicyProvider
{
    MovementPolicy CurrentPolicy { get; }
}
