using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// The requirement: gentler flaps, gentler lifts; stronger flaps, stronger lifts.
/// Each flight faces Vector3.forward, launches from rest, and is measured for 60 frames from the flap.
/// </summary>
public class FlapStrengthOver60FramesTests
{
    private static readonly float[] Speeds = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f };

    private readonly ITestOutputHelper output;
    public FlapStrengthOver60FramesTests(ITestOutputHelper output) => this.output = output;

    private static FlightTrace Fly(StrokeKind kind, StrokeShape shape, float speed) =>
        FlightRig.Fly(HandPaths.Stroke(kind, shape, speed));

    public static IEnumerable<object[]> AllStrokes()
    {
        foreach (StrokeKind kind in new[] { StrokeKind.DownOnly, StrokeKind.LiftThenDescend })
            foreach (StrokeShape shape in new[] { StrokeShape.Constant, StrokeShape.Bell })
                yield return new object[] { kind, shape };
    }

    public static IEnumerable<object[]> AllStrokesAtEverySpeed()
    {
        foreach (object[] stroke in AllStrokes())
            foreach (float speed in Speeds)
                yield return new[] { stroke[0], stroke[1], speed };
    }

    [Theory]
    [MemberData(nameof(AllStrokesAtEverySpeed))]
    public void Every_stroke_fires_a_flap(StrokeKind kind, StrokeShape shape, float speed)
    {
        Assert.True(Fly(kind, shape, speed).Flapped, $"{kind}/{shape} at {speed} m/s never fired a flap.");
    }

    [Theory]
    [MemberData(nameof(AllStrokesAtEverySpeed))]
    public void The_flap_fires_before_the_hands_stop(StrokeKind kind, StrokeShape shape, float speed)
    {
        FlightTrace trace = Fly(kind, shape, speed);

        Assert.True(trace.Flapped, $"{kind}/{shape} at {speed} m/s never fired a flap.");
        Assert.True(trace.FramesLateAfterHandsStopped <= 0,
            $"{kind}/{shape} at {speed} m/s: flap fired {trace.FramesLateAfterHandsStopped} frames after the hands stopped.");
    }

    [Theory]
    [MemberData(nameof(AllStrokesAtEverySpeed))]
    public void The_flight_rises_then_descends_within_60_frames(StrokeKind kind, StrokeShape shape, float speed)
    {
        FlightTrace trace = Fly(kind, shape, speed);
        Assert.True(trace.Flapped, $"{kind}/{shape} at {speed} m/s never fired a flap, so nothing lifted.");

        List<FrameSample> window = trace.Window();
        Assert.True(window[0].Velocity.y > 0f, $"Not rising on the flap frame: {window[0].Velocity}");
        Assert.True(trace.PeakHeight > 0f, "Never climbed above the launch point.");
        Assert.True(trace.PeakFrame < window.Count - 1, "Still climbing at the end of the window.");
        Assert.True(window[window.Count - 1].Velocity.y < 0f, $"Not falling by frame 60: {window[window.Count - 1].Velocity}");
    }

    [Theory]
    [MemberData(nameof(AllStrokes))]
    public void The_strongest_flap_climbs_higher_than_the_gentlest(StrokeKind kind, StrokeShape shape)
    {
        float gentlest = Fly(kind, shape, Speeds.First()).PeakHeight;
        float strongest = Fly(kind, shape, Speeds.Last()).PeakHeight;

        Assert.True(strongest > gentlest,
            $"{kind}/{shape}: {Speeds.Last()} m/s climbed {strongest:F2} m, {Speeds.First()} m/s climbed {gentlest:F2} m.");
    }

    [Theory]
    [MemberData(nameof(AllStrokes))]
    public void Every_step_up_in_strength_climbs_higher(StrokeKind kind, StrokeShape shape)
    {
        var results = Speeds.Select(s => (Speed: s, Peak: Fly(kind, shape, s).PeakHeight)).ToList();
        foreach (var r in results) output.WriteLine($"{r.Speed} m/s -> {r.Peak:F2} m");

        var violations = results.Zip(results.Skip(1))
            .Where(p => p.Second.Peak <= p.First.Peak)
            .Select(p => $"{p.First.Speed} m/s climbed {p.First.Peak:F2} m but {p.Second.Speed} m/s climbed {p.Second.Peak:F2} m")
            .ToList();

        Assert.True(violations.Count == 0, $"{kind}/{shape}: " + string.Join("; ", violations));
    }
}

/// <summary>Writes the synthetic data and the results to TestResults/ to inspect by hand. Asserts nothing.</summary>
public class FlapReport
{
    [Fact]
    public void Write_csv_files()
    {
        // Commas are the column separator, so decimals must use a point on every machine.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var summary = new StringBuilder("kind,shape,hand_speed,flapped,frames_late,smoothed_speed_at_flap,launch_y,launch_z,peak_height,peak_frame,height_at_frame_60\n");
        var frames = new StringBuilder("kind,shape,hand_speed,frame_since_flap,hand_height,height,velocity_y,velocity_z\n");

        foreach (StrokeKind kind in new[] { StrokeKind.DownOnly, StrokeKind.LiftThenDescend })
        foreach (StrokeShape shape in new[] { StrokeShape.Constant, StrokeShape.Bell })
        foreach (float speed in new[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f })
        {
            FlightTrace t = FlightRig.Fly(HandPaths.Stroke(kind, shape, speed));
            summary.Append($"{kind},{shape},{speed},{t.Flapped},{t.FramesLateAfterHandsStopped},{t.SmoothedDownSpeedAtFlap:F3},{t.LaunchVelocity.y:F3},{t.LaunchVelocity.z:F3},{t.PeakHeight:F3},{t.PeakFrame},{t.FinalHeight:F3}\n");

            List<FrameSample> window = t.Window();
            for (int i = 0; i < window.Count; i++)
            {
                float height = window[i].Position.y - (t.FlapFrames[0] > 0 ? t.Frames[t.FlapFrames[0] - 1].Position.y : 0f);
                frames.Append($"{kind},{shape},{speed},{i},{window[i].HandHeight:F3},{height:F3},{window[i].Velocity.y:F3},{window[i].Velocity.z:F3}\n");
            }
        }

        string dir = Path.Combine(ProjectDirectory(), "TestResults");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "flap_summary.csv"), summary.ToString());
        File.WriteAllText(Path.Combine(dir, "flap_60_frames.csv"), frames.ToString());
    }

    private static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path);
}
