using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>Fails if LevelOne's tuning changes, so the flight tests never drift from the shipped scene.</summary>
public class SceneConfigTests
{
    [Theory]
    [InlineData("gravity", LevelOne.Gravity)]
    [InlineData("glideStrength", LevelOne.GlideStrength)]
    public void The_harness_uses_the_values_set_in_LevelOne(string property, float expected)
    {
        string scene = File.ReadAllText(Path.Combine(ProjectDirectory(), "..", "Assets", "Scenes", "LevelOne.unity"));

        Match match = Regex.Match(scene, $@"propertyPath: {property}\s+value: (\S+)");

        Assert.True(match.Success, $"LevelOne.unity no longer overrides {property}.");
        Assert.Equal(expected, float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    private static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path);
}
