using System.Runtime.CompilerServices;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Configuration;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>
/// Prototype requirement: compare deep and shallow copies and report memory addresses;
/// the implementation can be switched (<c>Patterns:PrototypeCloneMode</c>, or <c>mode</c> here).
/// </summary>
public sealed class PrototypeDemo : IPatternDemo
{
    public string Key => "prototype";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var modeOption = options.Get("mode", "both");
        var modes = modeOption.Equals("both", StringComparison.OrdinalIgnoreCase)
            ? Enum.GetValues<CloneMode>()
            : [Enum.Parse<CloneMode>(modeOption, ignoreCase: true)];
        var trace = new DemoTrace("Prototype");
        var results = new Dictionary<string, object?>();

        foreach (var mode in modes)
        {
            var pristine = DemoWorld.TutorialLevel();
            var clone = pristine.Clone(mode);

            var rows = ObjectAddress.WhileGcPaused(() => Compare(pristine, clone));
            trace.Line($"{mode} clone of the tutorial level:");
            foreach (var row in rows)
            {
                trace.Line($"  {row.Part,-11} original {row.OriginalAddress} (hash {row.OriginalHash,9})  clone {row.CloneAddress} "
                           + $"(hash {row.CloneHash,9})  {(row.SameObject ? "SAME object" : "different object")}");
            }

            // Play on the clone, then restart from the pristine level the way the session does.
            var itemsBefore = pristine.Items.Count;
            var item = clone.Items[0];
            clone.RemoveItem(item);                       // a player picks up an item
            clone.Levers[0].IsActive = true;              // stands on a lever
            clone.Zombies.FirstOrDefault()?.TeleportTo(new GridPos(2, 2));
            var restarted = pristine.Clone(mode);
            var restoredOk = restarted.Items.Count == itemsBefore && !restarted.Levers[0].IsActive;
            trace.Line($"  Played on the clone (took {item.Id}, lever-1 on). Restart = pristine.Clone({mode}): "
                       + $"{restarted.Items.Count}/{itemsBefore} items, lever-1 {(restarted.Levers[0].IsActive ? "ON" : "off")} -> "
                       + (restoredOk ? "restored correctly." : "NOT restored: the shallow copy shared the lists and entities with the played level."));

            results[mode.ToString()] = new Dictionary<string, object?>
            {
                ["objects"] = rows,
                ["itemsAfterRestart"] = restarted.Items.Count,
                ["itemsInPristine"] = itemsBefore,
                ["restartRestoresLevel"] = restoredOk,
            };
        }

        trace.Evidence("modes", results)
            .Evidence("switch", "Patterns:PrototypeCloneMode = Deep | Shallow (appsettings), or ?mode= on this demo");
        return trace.Done("Deep: every mutable part is a new object and restart works. Shallow: only the level object is new; "
                          + "grid, lists and entities are shared, so playing corrupts the pristine copy.");
    }

    /// <summary>One compared part of the level.</summary>
    public sealed record PartComparison(string Part, string OriginalAddress, string CloneAddress, int OriginalHash, int CloneHash, bool SameObject);

    /// <summary>Addresses and identity hashes of the level and its parts in the original and the clone.</summary>
    public static PartComparison[] Compare(LevelState original, LevelState clone)
    {
        (string, object, object)[] parts =
        [
            ("level", original, clone),
            ("grid", original.Grid, clone.Grid),
            ("items list", original.Items, clone.Items),
            ("item[0]", original.Items[0], clone.Items[0]),
            ("lever[0]", original.Levers[0], clone.Levers[0]),
            ("door", original.Door!, clone.Door!),
            ("tile (1,1)", original.Grid.GetTile(new GridPos(1, 1)), clone.Grid.GetTile(new GridPos(1, 1))),
            ("definition", original.Definition, clone.Definition),
        ];
        return parts.Select(p => new PartComparison(p.Item1, ObjectAddress.Format(p.Item2), ObjectAddress.Format(p.Item3),
            RuntimeHelpers.GetHashCode(p.Item2), RuntimeHelpers.GetHashCode(p.Item3), ReferenceEquals(p.Item2, p.Item3))).ToArray();
    }
}
