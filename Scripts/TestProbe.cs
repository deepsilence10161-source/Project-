using Godot;

/// <summary>
/// Machine-readable game events for the emulator E2E test.
///
/// Godot draws the whole game into one native surface, so no UI-automation
/// tool can see inside it. Instead the game itself reports what happened
/// (state changes, lane moves, jumps, crashes, a periodic heartbeat) as
/// single log lines:  <c>ND_EVT &lt;name&gt; key=value ...</c>
/// Android routes GD.Print to logcat, where the CI script asserts on them.
///
/// Debug builds only: release APKs print nothing.
/// </summary>
public static class TestProbe
{
    private static readonly bool Enabled = OS.IsDebugBuild();

    public static void Emit(string name, string details = "")
    {
        if (!Enabled) return;
        GD.Print(details.Length == 0 ? $"ND_EVT {name}" : $"ND_EVT {name} {details}");
    }
}
