using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace KeyboardSwitcher;

internal sealed class F1Diagnostics {
    private readonly ConcurrentQueue<object> events = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private int eventCount;
    internal bool Complete => clock.Elapsed >= TimeSpan.FromSeconds(120);
    internal int EventCount => events.Count;

    private void Add(object entry) {
        if (Complete || Interlocked.Increment(ref eventCount) > 128) return;
        events.Enqueue(entry);
    }

    internal void Configure(bool enabled, bool compatibility, bool outputSucceeded) => Add(new {
        elapsedMs = clock.ElapsedMilliseconds, kind = "configure", enabled, compatibility, outputSucceeded
    });

    internal void Key(uint key, uint scan, uint flags, bool ownOutput, bool down, string action,
        IReadOnlyList<Stroke>? output = null, bool? outputSucceeded = null) {
        if (key is not (0x70 or 0xAD)) return;
        Add(new {
            elapsedMs = clock.ElapsedMilliseconds, kind = "key", key, scan, flags, ownOutput, down,
            action, output, outputSucceeded
        });
    }

    internal void Write(string path, string settingsPath, bool functionMode, bool compatibility, string? error, bool stopped = false) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(new {
            processId = Environment.ProcessId, executable = Environment.ProcessPath,
            settingsPath, functionMode, compatibility, error,
            captureComplete = Complete || stopped, elapsedMs = clock.ElapsedMilliseconds,
            events = events.ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
