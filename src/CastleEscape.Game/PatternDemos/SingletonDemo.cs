using System.Runtime.CompilerServices;
using CastleEscape.Contracts.Patterns;
using CastleEscape.Game.Content;

namespace CastleEscape.Game.PatternDemos;

/// <summary>
/// Demo-only: a lazy "singleton" WITHOUT thread safety, to show the race the real one avoids.
/// The constructor is slow, like loading files, which widens the race window.
/// </summary>
public sealed class NaiveCatalogHolder
{
    private static NaiveCatalogHolder? _instance;
    private static int _constructions;

    private NaiveCatalogHolder()
    {
        Interlocked.Increment(ref _constructions);
        Thread.Sleep(20); // pretend to read the content files
    }

    public static int Constructions => Volatile.Read(ref _constructions);

    // Check-then-create with no lock: two threads can both see null and both construct.
    public static NaiveCatalogHolder Instance => _instance ??= new NaiveCatalogHolder();

    public static void Reset()
    {
        _instance = null;
        _constructions = 0;
    }
}

/// <summary>Singleton requirement: show that ContentCatalog.Instance is thread-safe.</summary>
public sealed class SingletonDemo : IPatternDemo
{
    public string Key => "singleton";

    public PatternDemoResponse Run(DemoOptions options)
    {
        var threads = Math.Clamp(options.GetInt("threads", 64), 2, 512);
        var includeNaive = options.GetBool("naive", true);
        var trace = new DemoTrace("Singleton");

        var creationsBefore = ContentCatalog.InstanceCreations;
        var ids = RaceThreads(threads, () => RuntimeHelpers.GetHashCode(ContentCatalog.Instance));
        var distinct = ids.Distinct().Count();
        trace.Line($"{threads} threads waited at a Barrier, then all read ContentCatalog.Instance at once.")
            .Line($"Distinct instances seen: {distinct} (identity hash {ids[0]}).")
            .Line($"Instance created {ContentCatalog.InstanceCreations} time(s) in total "
                  + $"({ContentCatalog.InstanceCreations - creationsBefore} during this run; 0 means it already existed).")
            .Evidence("threads", threads)
            .Evidence("distinctInstances", distinct)
            .Evidence("instanceCreations", ContentCatalog.InstanceCreations)
            .Evidence("identityHash", ids[0]);

        if (includeNaive)
        {
            NaiveCatalogHolder.Reset();
            var naiveIds = RaceThreads(threads, () => RuntimeHelpers.GetHashCode(NaiveCatalogHolder.Instance));
            var naiveDistinct = naiveIds.Distinct().Count();
            trace.Line($"Same race against NaiveCatalogHolder (no lock): {naiveDistinct} distinct instances, "
                       + $"{NaiveCatalogHolder.Constructions} constructor calls.")
                .Evidence("naiveDistinctInstances", naiveDistinct)
                .Evidence("naiveConstructorCalls", NaiveCatalogHolder.Constructions);
        }

        return trace.Done(distinct == 1
            ? "Thread-safe: every thread got the same ContentCatalog, created once."
            : "NOT thread-safe: more than one ContentCatalog was seen.");
    }

    /// <summary>Starts all threads together with a Barrier and collects what each one saw.</summary>
    private static int[] RaceThreads(int count, Func<int> read)
    {
        var results = new int[count];
        using var barrier = new Barrier(count);
        var workers = Enumerable.Range(0, count).Select(i => new Thread(() =>
        {
            barrier.SignalAndWait();
            results[i] = read();
        })).ToList();

        workers.ForEach(t => t.Start());
        workers.ForEach(t => t.Join());
        return results;
    }
}
