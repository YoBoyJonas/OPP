using CastleEscape.Contracts;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Content;
using CastleEscape.Game.Generation.Themes;
using CastleEscape.Game.Powers;
using CastleEscape.Game.World;

namespace CastleEscape.Game.PatternDemos;

/// <summary>Decorator requirement: at least 3 decoration levels.</summary>
public sealed class DecoratorDemo : IPatternDemo
{
    public string Key => "decorator";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var catalog = DemoWorld.Catalog;
        var characterId = options.Get("character", "scout");
        var character = catalog.GetCharacter(characterId);
        var sprintPickups = Math.Clamp(options.GetInt("sprints", 2), 0, 3);
        var theme = ThemeFactories.For(LevelTheme.Dungeon, catalog);
        var floor = new Tile(TerrainKind.Floor);
        var water = new Tile(TerrainKind.Water, theme.CreateWater());
        var pit = new Tile(TerrainKind.Pit, theme.CreatePit());
        var powers = new PowerManager(character);
        var trace = new DemoTrace("Decorator");
        var layers = new List<Dictionary<string, object?>>();

        void Show(string action)
        {
            var a = powers.Abilities;
            string Rate(Tile tile) => a.CanEnter(tile.Terrain) ? $"{a.SpeedOn(tile):0.00}" : "closed";
            trace.Line($"{action,-22} depth {AbilityChain.Depth(a)}: {a.Describe()}");
            trace.Line($"{"",22}         floor {Rate(floor)}  water {Rate(water)}  pit {Rate(pit)} tiles/s");
            layers.Add(new()
            {
                ["after"] = action,
                ["chain"] = a.Describe(),
                ["depth"] = AbilityChain.Depth(a),
                ["floor"] = Math.Round(a.SpeedOn(floor), 3),
                ["water"] = a.CanEnter(TerrainKind.Water) ? Math.Round(a.SpeedOn(water), 3) : null,
                ["pit"] = a.CanEnter(TerrainKind.Pit) ? Math.Round(a.SpeedOn(pit), 3) : null,
            });
        }

        PowerGrant Grant(PowerType type) => catalog.Consumables.First(c => c.Grant?.Power == type).Grant!;
        void Pick(PowerType type)
        {
            powers.Gain(Grant(type), 10, maxLevel: 3);
            powers.RefreshCombos(catalog.Combos);
            Show($"+ {type}");
        }

        Show($"{character.Name} (no powers)");
        for (var i = 0; i < sprintPickups; i++) Pick(PowerType.Sprint);
        Pick(PowerType.Jump);
        Pick(PowerType.Swim);

        powers.Tick(10 * (sprintPickups + 1)); // every timer runs out (stacked pickups add time)
        powers.RefreshCombos(catalog.Combos);
        Show("all expired");

        var maxDepth = layers.Max(l => (int)l["depth"]!);
        trace.Evidence("layers", layers).Evidence("maxDepth", maxDepth);
        return trace.Done($"Up to {maxDepth} decorators around the character; MovementRules and the tick loop only see IAbilities.");
    }
}
