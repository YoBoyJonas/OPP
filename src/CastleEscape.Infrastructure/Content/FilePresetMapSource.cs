using CastleEscape.Domain.Generation;

namespace CastleEscape.Infrastructure.Content;

/// <summary>The preset map files in <c>content/presets</c>.</summary>
public sealed class FilePresetMapSource : IPresetMapSource
{
    public string Directory => Path.Combine(ContentLoader.DefaultDirectory, "presets");

    /// <summary>Names of the available presets (file names without .txt).</summary>
    public IReadOnlyList<string> Available() =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.GetFiles(Directory, "*.txt").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order().ToList()
            : [];

    public string[] ReadRows(string name)
    {
        var path = Path.Combine(Directory, name + ".txt");
        if (!File.Exists(path))
        {
            throw new LevelBuildException($"Preset '{name}' not found. Available: {string.Join(", ", Available())}");
        }
        return File.ReadAllLines(path).Where(r => r.Length > 0).ToArray();
    }
}
