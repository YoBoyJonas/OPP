using CastleEscape.Game.Content;

namespace CastleEscape.Game.Generation;

public static class PresetMaps
{
    public static string Directory => Path.Combine(ContentLoader.DefaultDirectory, "presets");

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
