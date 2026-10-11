namespace CastleEscape.Domain.Generation;

/// <summary>Supplies preset rows without coupling construction to the file system.</summary>
public interface IPresetMapSource
{
    IReadOnlyList<string> Available();
    string[] ReadRows(string name);
}
