using System;
using UnityEngine;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)] // Time is static, as in Unity.

/// <summary>FlightPhysics.CalculateFlapVelocity on its own, with Movement's real arguments.</summary>
public class FlightPhysicsVectorTests
{
    // Movement.FireFlap passes these.
    private const float FlapMagnitude = 3f, FlapStrength = 5f, ForwardThrust = 1f;

    [Fact]
    public void Facing_forward_the_base_flap_vector_is_0_15_3()
    {
        Vector3 v = FlightPhysics.CalculateFlapVelocity(Vector3.forward, FlapMagnitude, FlapStrength, ForwardThrust);

        Assert.Equal(0f, v.x, 4);
        Assert.Equal(15f, v.y, 4);
        Assert.Equal(3f, v.z, 4);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(9f)]
    public void A_larger_magnitude_gives_proportionally_more_lift(float magnitude)
    {
        Vector3 v = FlightPhysics.CalculateFlapVelocity(Vector3.forward, magnitude, FlapStrength, ForwardThrust);

        Assert.Equal(FlapStrength * magnitude, v.y, 4);
        Assert.Equal(ForwardThrust * magnitude, v.z, 4);
    }

    [Fact]
    public void The_head_direction_is_normalized_so_its_length_does_not_change_the_flap()
    {
        Vector3 unit = FlightPhysics.CalculateFlapVelocity(Vector3.forward, FlapMagnitude, FlapStrength, ForwardThrust);
        Vector3 longer = FlightPhysics.CalculateFlapVelocity(Vector3.forward * 7f, FlapMagnitude, FlapStrength, ForwardThrust);

        Assert.Equal(unit.y, longer.y, 4);
        Assert.Equal(unit.z, longer.z, 4);
    }
}

/// <summary>Synthetic hand movement through the real HandVelocityTracker and Movement.</summary>
public class HandMovementToVectorTests
{
    [Fact]
    public void Still_hands_do_not_flap()
    {
        FlightTrace trace = FlightRig.Fly(HandPaths.Still(120), FlightMode.FlapVectorOnly);

        Assert.False(trace.Flapped);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(5f)]
    public void Raising_the_hands_does_not_flap(float speed)
    {
        FlightTrace trace = FlightRig.Fly(HandPaths.RiseOnly(speed), FlightMode.FlapVectorOnly);

        Assert.False(trace.Flapped);
    }

    [Fact]
    public void A_downstroke_below_the_half_metre_per_second_threshold_does_not_flap()
    {
        float[] path = HandPaths.Stroke(StrokeKind.DownOnly, StrokeShape.Constant, 0.4f);

        Assert.False(FlightRig.Fly(path, FlightMode.FlapVectorOnly).Flapped);
    }

    [Theory]
    [InlineData(StrokeShape.Constant, 1f)]
    [InlineData(StrokeShape.Constant, 3f)]
    [InlineData(StrokeShape.Constant, 6f)]
    [InlineData(StrokeShape.Constant, 9f)]
    [InlineData(StrokeShape.Bell, 1f)]
    [InlineData(StrokeShape.Bell, 3f)]
    [InlineData(StrokeShape.Bell, 6f)]
    [InlineData(StrokeShape.Bell, 9f)]
    public void One_downstroke_fires_exactly_one_flap(StrokeShape shape, float speed)
    {
        float[] path = HandPaths.Stroke(StrokeKind.DownOnly, shape, speed);

        Assert.Equal(1, FlightRig.Fly(path, FlightMode.FlapVectorOnly).FlapCount);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(6f)]
    [InlineData(9f)]
    public void The_flap_vector_always_points_along_0_15_3(float speed)
    {
        float[] path = HandPaths.Stroke(StrokeKind.DownOnly, StrokeShape.Constant, speed);

        Vector3 launch = FlightRig.Fly(path, FlightMode.FlapVectorOnly).LaunchVelocity;

        Vector3 expected = new Vector3(0f, 15f, 3f).normalized;
        Assert.True((launch.normalized - expected).magnitude < 1e-3f, $"launch {launch} is not along {expected}");
    }

    /// <summary>
    /// The contract Movement.cs declares: "full hand-down at 10 m/s", with a stroke multiplier
    /// running from 1 to strokeMaxMul (5). So a stroke at speed s should launch at
    /// Lerp(1, 5, s/10) x (0, 15, 3). Allows 25% to cover the smoothing lag.
    /// </summary>
    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(5f)]
    [InlineData(7f)]
    [InlineData(9f)]
    public void The_lift_matches_the_declared_scale_for_the_hand_speed(float speed)
    {
        float[] path = HandPaths.Stroke(StrokeKind.DownOnly, StrokeShape.Constant, speed);

        FlightTrace trace = FlightRig.Fly(path, FlightMode.FlapVectorOnly);

        float expectedLift = Mathf.Lerp(1f, 5f, speed / 10f) * 15f;
        float actualLift = trace.LaunchVelocity.y;
        Assert.True(MathF.Abs(actualLift - expectedLift) <= expectedLift * 0.25f,
            $"Hands at {speed} m/s: expected lift of about {expectedLift:F1} m/s, got {actualLift:F1} m/s " +
            $"(the tracker read {trace.SmoothedDownSpeedAtFlap:F2} m/s when the flap fired).");
    }
}
