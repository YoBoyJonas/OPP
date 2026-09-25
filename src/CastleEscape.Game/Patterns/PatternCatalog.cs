using System.Reflection;
using CastleEscape.Contracts.Patterns;

namespace CastleEscape.Game.Patterns;

/// <summary>
/// The 12 P1 patterns: what each solves here and who owns it. Participants come from
/// <see cref="DesignPatternAttribute"/> by reflection, so they always match the code.
/// </summary>
public static class PatternCatalog
{
    private sealed record Info(string Key, string Name, string Category, string Owner, string Problem, string Requirement, string HowMet);

    private const string A = "Student A (world creation)";
    private const string B = "Student B (gameplay rules)";
    private const string C = "Student C (infrastructure / networking)";
    private const string D = "Student D (session orchestration)";

    private static readonly Info[] Infos =
    [
        new("singleton", "Singleton", "Creational", C,
            "Content is loaded from JSON once and must be one shared, immutable instance for all sessions, generators and endpoints.",
            "Show that it is thread-safe.",
            "ContentCatalog.Instance uses Lazy<T> with ExecutionAndPublication. The demo starts N threads through a Barrier: 1 distinct instance, 1 creation. A naive holder in the same demo creates several."),
        new("factory-method", "Factory Method", "Creational", B,
            "The level builder must place different item kinds without knowing the concrete item classes.",
            "At least 3 classes in the product family.",
            "ItemSpawner.Spawn() calls the factory method CreateItem(); HealthItemSpawner, RewardItemSpawner and PowerItemSpawner create HealthItem, RewardItem and PowerItem."),
        new("abstract-factory", "Abstract Factory", "Creational", A,
            "Levels 1-5 are Dungeon, 6-10 Crypt. Each theme needs a consistent family of walls, water, pits and zombies; mixing families is a bug.",
            "At least 2 concrete factories, at least 3 classes per family.",
            "DungeonThemeFactory and CryptThemeFactory each create a family of 4: wall, water, pit and zombie, with real gameplay differences."),
        new("builder", "Builder", "Creational", A,
            "A level has many ordered construction steps with constraints (GEN-1..3, D5), and needs both random and hand-made deterministic variants.",
            "At least 2 concrete builders.",
            "LevelDirector runs the same steps on ProceduralLevelBuilder and PresetLevelBuilder, validates, and retries with a new seed."),
        new("prototype", "Prototype", "Creational", A,
            "Restart must restore exactly the generated layout (items back, zombies home, levers off, door closed) without regenerating.",
            "Compare deep and shallow copies and report memory addresses; be able to switch the implementation at the defence.",
            "LevelState.DeepClone/ShallowClone; the demo reports ReferenceEquals, hash codes and addresses, then shows the shallow-copy restart bug. Switch: Patterns:PrototypeCloneMode."),
        new("strategy", "Strategy", "Behavioral", B,
            "Zombie types chase differently, and the behaviour must be swappable at runtime.",
            "At least 4 strategy classes.",
            "Greedy, BFS, A* and Predictive chase strategies behind IZombieMovementStrategy; swappable per zombie at runtime."),
        new("decorator", "Decorator", "Structural", B,
            "Powers are temporary, stack, and combine into super powers: abilities = base character + whatever is active now.",
            "At least 3 decoration levels.",
            "PowerManager rebuilds CharacterAbilities wrapped in one decorator per power level plus combo decorators, e.g. FastSwim(Sprint(Sprint(Swim(base))))."),
        new("observer", "Observer", "Behavioral", D,
            "Clients, statistics, logs and sound cues all react to the same gameplay facts; the simulation must not know about them.",
            "Show how it works with a sequence diagram.",
            "GameEventPublisher notifies ClientNotificationObserver, SessionStatisticsObserver, EventLogObserver and SoundCueObserver. Sequence diagram in docs/patterns/Observer.md."),
        new("facade", "Facade", "Structural", D,
            "The hub, REST endpoints and the console demo would each have to orchestrate registry, generation, loop and commands themselves.",
            "At least 2 client classes and at least 3 subsystem classes.",
            "GameFacade is used by GameHub, SessionEndpoints, GameplayEndpoints and the console demo, over SessionRegistry, LevelDirector, CommandProcessor, GameLoopScheduler and ContentCatalog."),
        new("command", "Command", "Behavioral", D,
            "Inputs arrive asynchronously and must be applied at a fixed point in the tick; some must be reverted (conflicts, restart mistakes, debugging).",
            "Commands must support undo().",
            "IGameCommand.Execute/Undo; CommandProcessor queues, executes and keeps history. The later of two same-tile steps is undone (D7); Dev undo-last-command."),
        new("adapter", "Adapter", "Structural", C,
            "NET-2 allows JSON or XML; the rest of the code needs one small interface, but the two .NET serializers have large, incompatible APIs.",
            "Adapter and adaptee have different numbers of methods.",
            "IMessageSerializer (3 members) adapts System.Text.Json.JsonSerializer and DataContractSerializer, which have many more public methods (counted in the demo)."),
        new("bridge", "Bridge", "Structural", C,
            "Two independent axes, kind of update (state or events) and delivery channel (SignalR or polling), would multiply into classes.",
            "At least 2 abstractions and 2 implementations; explain the difference from Strategy and Adapter.",
            "StateNotifier and EventNotifier (abstractions) over SignalRClientChannel and PollingBufferChannel (implementors): 2 + 2 classes cover all 4 combinations."),
    ];

    public static IEnumerable<string> Keys => Infos.Select(i => i.Key);

    /// <summary>Every pattern with its participants found in the given assemblies.</summary>
    public static PatternDto[] Describe(params Assembly[] assemblies)
    {
        var participants = assemblies
            .SelectMany(SafeTypes)
            .SelectMany(t => t.GetCustomAttributes<DesignPatternAttribute>().Select(a => (Type: t, Attribute: a)))
            .ToList();

        return Infos.Select(info => new PatternDto(
            info.Key, info.Name, info.Category, info.Owner, info.Problem, info.Requirement, info.HowMet,
            $"docs/patterns/{info.Name.Replace(" ", "")}.md",
            $"/api/patterns/{info.Key}/demo",
            participants
                .Where(p => p.Attribute.Pattern == info.Name)
                .OrderBy(p => p.Attribute.Role).ThenBy(p => p.Type.Name)
                .Select(p => new PatternParticipantDto(TypeName(p.Type), p.Attribute.Role, p.Attribute.SourceFile))
                .ToArray())).ToArray();
    }

    private static string TypeName(Type type) =>
        type.IsGenericType ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(",", type.GetGenericArguments().Select(a => a.Name))}>" : type.Name;

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
