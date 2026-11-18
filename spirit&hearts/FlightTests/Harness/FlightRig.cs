using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Tuning that the shipped scene sets on the Movement component. See SceneConfigTests.</summary>
public static class LevelOne
{
    public const float Gravity = 70f;
    public const float GlideStrength = 25f;
}

public enum FlightMode
{
    /// <summary>Flap only: no gravity or glide, so the flap vector can be read directly.</summary>
    FlapVectorOnly,
    /// <summary>Full game physics. Gravity starts on the flap frame, so every stroke launches from rest.</summary>
    FullPhysics,
}

public struct FrameSample
{
    public int Frame;
    public float HandHeight;
    public Vector3 Position;
    public Vector3 Velocity;
}

/// <summary>The result of flying one hand path.</summary>
public sealed class FlightTrace
{
    public readonly List<FrameSample> Frames = new List<FrameSample>();
    public readonly List<int> FlapFrames = new List<int>();

    /// <summary>The tracker's smoothed down speed that was read when the first flap fired.</summary>
    public float SmoothedDownSpeedAtFlap;

    /// <summary>The last frame of the hand path. After this the hands are not moving.</summary>
    public int StrokeEndFrame;

    /// <summary>How many frames after the hands stopped the first flap fired. Negative means mid-stroke.</summary>
    public int FramesLateAfterHandsStopped => Flapped ? FlapFrames[0] - StrokeEndFrame : 0;

    public bool Flapped => FlapFrames.Count > 0;
    public int FlapCount => FlapFrames.Count;

    /// <summary>Velocity at the end of the frame the first flap fired on.</summary>
    public Vector3 LaunchVelocity => Flapped ? Frames[FlapFrames[0]].Velocity : Vector3.zero;

    /// <summary>The (up to) 60 frames starting at the first flap. Empty if no flap fired.</summary>
    public List<FrameSample> Window()
    {
        var window = new List<FrameSample>();
        if (Flapped)
            for (int i = FlapFrames[0]; i < Frames.Count && i < FlapFrames[0] + FlightRig.WindowFrames; i++)
                window.Add(Frames[i]);
        return window;
    }

    private float StartHeight => Flapped && FlapFrames[0] > 0 ? Frames[FlapFrames[0] - 1].Position.y : 0f;

    /// <summary>Highest climb above the launch point in the window. Zero if no flap fired.</summary>
    public float PeakHeight
    {
        get
        {
            float peak = 0f;
            foreach (var f in Window()) peak = Mathf.Max(peak, f.Position.y - StartHeight);
            return peak;
        }
    }

    /// <summary>Frames from the flap to the top of the climb.</summary>
    public int PeakFrame
    {
        get
        {
            var window = Window();
            int best = 0;
            for (int i = 1; i < window.Count; i++)
                if (window[i].Position.y > window[best].Position.y) best = i;
            return best;
        }
    }

    public float FinalHeight
    {
        get
        {
            var window = Window();
            return window.Count == 0 ? 0f : window[window.Count - 1].Position.y - StartHeight;
        }
    }
}

/// <summary>
/// Drives the real Movement and HandVelocityTracker through a hand path, one frame at a time,
/// in Unity's order: poses update, then Update, then LateUpdate. The head looks along Vector3.forward.
/// </summary>
public sealed class FlightRig
{
    public const float DeltaTime = 1f / 60f;
    public const int WindowFrames = 60;

    /// <summary>Long enough to fill the tracker's 20-frame history with "not moving".</summary>
    private const int SettleFrames = 25;

    private const float HeadHeight = 1.6f;
    private const float HandHalfSpread = 0.5f;
    private const float RestingHandHeight = 1.2f;

    private readonly Movement movement;
    private readonly HandVelocityTracker leftTracker, rightTracker;
    private readonly Transform rig, head, leftHand, rightHand;
    private readonly TestPolicy policy;
    private readonly FlightMode mode;
    private readonly FlightTrace trace = new FlightTrace();

    private int frame;
    private float handHeight;

    private FlightRig(FlightMode mode, float startHandHeight)
    {
        this.mode = mode;
        handHeight = startHandHeight;
        Time.time = 0f;
        Time.deltaTime = DeltaTime;

        var player = new GameObject();
        rig = player.transform;
        head = new GameObject().transform;
        var leftObject = new GameObject();
        var rightObject = new GameObject();
        leftHand = leftObject.transform;
        rightHand = rightObject.transform;

        policy = player.AddComponent<TestPolicy>();
        policy.CurrentPolicy = mode == FlightMode.FlapVectorOnly
            ? new MovementPolicy { Allowed = MovementAbility.Flap | MovementAbility.Translate, GravityEnabled = false }
            : new MovementPolicy { Allowed = MovementPolicy.Everything.Allowed, GravityEnabled = false };

        player.AddComponent<TutorialManager>();
        movement = player.AddComponent<Movement>();

        leftTracker = leftObject.AddComponent<HandVelocityTracker>();
        rightTracker = rightObject.AddComponent<HandVelocityTracker>();
        SetPrivate(leftTracker, "headReference", head);
        SetPrivate(rightTracker, "headReference", head);

        movement.leftHand = leftHand;
        movement.rightHand = rightHand;
        movement.head = head;
        movement.rigRoot = rig;
        movement.leftVelocity = leftTracker;
        movement.rightVelocity = rightTracker;
        SetPrivate(movement, "gravity", LevelOne.Gravity);
        SetPrivate(movement, "glideStrength", LevelOne.GlideStrength);

        movement.OnFlap += HandleFlap;

        PlaceTransforms();
        Call(movement, "Awake");
        Call(movement, "Start");
    }

    /// <summary>Settles, plays the hand path, then holds still until 60 frames have passed since the first flap.</summary>
    public static FlightTrace Fly(float[] handPath, FlightMode mode = FlightMode.FullPhysics)
    {
        // Settle at the path's first height. Starting anywhere else would teleport the hands
        // on the first frame, and that spike would stay in the tracker's average for 20 frames.
        float start = handPath.Length > 0 ? handPath[0] : 0f;
        var flight = new FlightRig(mode, start);
        for (int i = 0; i < SettleFrames; i++) flight.Step(start);
        foreach (float y in handPath) flight.Step(y);
        flight.trace.StrokeEndFrame = flight.frame - 1;

        // If no flap has fired by now, one more window is enough to prove it never will.
        float rest = handPath.Length > 0 ? handPath[handPath.Length - 1] : 0f;
        int stopAt = flight.frame + WindowFrames;
        while (flight.frame < stopAt)
        {
            flight.Step(rest);
            if (flight.trace.Flapped && flight.frame >= flight.trace.FlapFrames[0] + WindowFrames) break;
        }
        return flight.trace;
    }

    private void Step(float newHandHeight)
    {
        handHeight = newHandHeight;
        Time.time += DeltaTime;

        PlaceTransforms();
        Call(movement, "Update");
        PlaceTransforms(); // The hands and head are children of the rig, which Update just moved.
        Call(leftTracker, "LateUpdate");
        Call(rightTracker, "LateUpdate");

        trace.Frames.Add(new FrameSample
        {
            Frame = frame,
            HandHeight = handHeight,
            Position = rig.position,
            Velocity = movement.CurrentVelocity,
        });
        frame++;
    }

    private void HandleFlap()
    {
        if (!trace.Flapped)
            trace.SmoothedDownSpeedAtFlap = -(leftTracker.SmoothedVelocity.y + rightTracker.SmoothedVelocity.y) * 0.5f;
        trace.FlapFrames.Add(frame);

        if (mode == FlightMode.FullPhysics)
            policy.CurrentPolicy = MovementPolicy.Everything;
    }

    private void PlaceTransforms()
    {
        Vector3 root = rig.position;
        head.position = root + new Vector3(0f, HeadHeight, 0f);
        leftHand.position = root + new Vector3(-HandHalfSpread, RestingHandHeight + handHeight, 0f);
        rightHand.position = root + new Vector3(HandHalfSpread, RestingHandHeight + handHeight, 0f);
    }

    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, Any).SetValue(target, value);

    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, Any).Invoke(target, null);
}

public class TestPolicy : MonoBehaviour, IMovementPolicyProvider
{
    public MovementPolicy CurrentPolicy { get; set; }
}
