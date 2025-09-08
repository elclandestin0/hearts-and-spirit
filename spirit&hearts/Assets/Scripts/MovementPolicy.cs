// MovementPolicy.cs
using System;

[Flags]
public enum MovementAbility
{
    None  = 0,
    Look  = 1 << 0,
    Flap  = 1 << 1,
    Glide = 1 << 2,
    Dive  = 1 << 3,
    Hover = 1 << 4,
    Translate = 1 << 5
}

public struct MovementPolicy
{
    public MovementAbility Allowed;
    public bool GravityEnabled;

    public static MovementPolicy Everything =>
        new MovementPolicy {
            Allowed = (MovementAbility)(-1),
            GravityEnabled = true
        };
}

public interface IMovementPolicyProvider
{
    MovementPolicy CurrentPolicy { get; }
}
