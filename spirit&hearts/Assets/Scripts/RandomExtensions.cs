using System;

public static class RandomExtensions
{
    public static float NextFloat(this Random rand, float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }
}