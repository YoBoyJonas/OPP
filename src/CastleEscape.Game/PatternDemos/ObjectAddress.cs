using System.Runtime;
using System.Runtime.CompilerServices;

namespace CastleEscape.Game.PatternDemos;

/// <summary>
/// Reads the memory address of an object, for the Prototype requirement "report memory addresses".
/// The garbage collector may move objects, so addresses are read inside a no-GC region and
/// only compared with each other while it lasts.
/// </summary>
public static class ObjectAddress
{
    /// <summary>The object's current address on the managed heap (the reference itself, read as a number).</summary>
    public static nint Of(object obj) => Unsafe.As<object, nint>(ref obj);

    public static string Format(object obj) => $"0x{Of(obj):X}";

    /// <summary>Runs <paramref name="read"/> while the GC is paused, so the addresses it reads stay valid together.</summary>
    public static T WhileGcPaused<T>(Func<T> read)
    {
        var paused = false;
        try
        {
            paused = GC.TryStartNoGCRegion(4 * 1024 * 1024);
        }
        catch (InvalidOperationException)
        {
            // Already inside a no-GC region (another demo running at the same time); read anyway.
        }
        try
        {
            return read();
        }
        finally
        {
            if (paused && GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
            {
                GC.EndNoGCRegion();
            }
        }
    }
}
