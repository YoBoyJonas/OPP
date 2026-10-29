using CastleEscape.Game.Content;

namespace CastleEscape.Game.Generation;

/// <summary>The hand-made map files in <c>content/presets</c>, read by <see cref="PresetLevelBuilder"/>.</summary>
public static class PresetMaps
{
    public static string Directory => Path.Combine(ContentLoader.DefaultDirectory, "presets");

    /// <summary>Names of the available presets (file names without .txt).</summary>
    public static IReadOnlyList<string> Available() =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.GetFiles(Directory, "*.txt").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order().ToList()
            : [];

    public static string[] ReadRows(string name)
    {
        var path = Path.Combine(Directory, name + ".txt");
        if (!File.Exists(path))
        {
            throw new LevelBuildException($"Preset '{name}' not found. Available: {string.Join(", ", Available())}");
        }
        return File.ReadAllLines(path).Where(r => r.Length > 0).ToArray();
    }
}
