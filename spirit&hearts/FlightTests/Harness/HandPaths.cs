using System;
using System.Collections.Generic;

/// <summary>How the hands move through the stroke.</summary>
public enum StrokeShape
{
    /// <summary>Same speed for the entire stroke.</summary>
    Constant,
    /// <summary>Eases in and out like a real arm. The speed given is the peak.</summary>
    Bell,
}

/// <summary>Where the stroke starts.</summary>
public enum StrokeKind
{
    /// <summary>Hands are already raised and only come down.</summary>
    DownOnly,
    /// <summary>Hands rise from rest, then come back down to where they started.</summary>
    LiftThenDescend,
}

/// <summary>Synthetic hand data: the height of both hands, per frame, relative to the rig.</summary>
public static class HandPaths
{
    /// <summary>Roughly a full arm sweep, in metres.</summary>
    public const float DefaultAmplitude = 0.8f;

    public static float[] Still(int frames) => new float[frames];

    public static float[] Stroke(StrokeKind kind, StrokeShape shape, float speed, float amplitude = DefaultAmplitude)
    {
        float[] rise = shape == StrokeShape.Constant ? ConstantRise(speed, amplitude) : BellRise(speed, amplitude);
        float top = rise[rise.Length - 1];

        var path = new List<float>();
        if (kind == StrokeKind.LiftThenDescend)
            path.AddRange(rise);
        else
            path.Add(top);

        // The way down mirrors the way up, ending back at the starting height.
        for (int i = rise.Length - 2; i >= 0; i--)
            path.Add(rise[i]);
        path.Add(0f);
        return path.ToArray();
    }

    public static float[] RiseOnly(float speed, float amplitude = DefaultAmplitude) => ConstantRise(speed, amplitude);

    private static float[] ConstantRise(float speed, float amplitude)
    {
        float step = speed * FlightRig.DeltaTime;
        int frames = Math.Max(1, (int)MathF.Round(amplitude / step));
        var path = new float[frames];
        for (int i = 0; i < frames; i++)
            path[i] = step * (i + 1);
        return path;
    }

    private static float[] BellRise(float peakSpeed, float amplitude)
    {
        // y = A(1 - cos(pi*t/T))/2 has a peak speed of A*pi/(2T).
        float duration = amplitude * MathF.PI / (2f * peakSpeed);
        int frames = Math.Max(1, (int)MathF.Ceiling(duration / FlightRig.DeltaTime));
        var path = new float[frames];
        for (int i = 0; i < frames; i++)
            path[i] = amplitude * (1f - MathF.Cos(MathF.PI * (i + 1) / frames)) * 0.5f;
        return path;
    }
}
