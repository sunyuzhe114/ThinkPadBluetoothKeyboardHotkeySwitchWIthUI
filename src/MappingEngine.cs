namespace KeyboardSwitcher;

public readonly record struct KeyEvent(int Key, bool Down, bool Injected = false);
public readonly record struct Stroke(int Key, bool Down);
public sealed record Decision(bool Suppress, IReadOnlyList<Stroke> Output) {
    public static readonly Decision Pass = new(false, Array.Empty<Stroke>());
}

public sealed class MappingEngine {
    private sealed record Press(int Target, int[] Added, int[] Removed);
    private static readonly int[] Media = [0xAD, 0xAE, 0xAF, 0xB1, 0xB3, 0xB0, 0xA6, 0xAC, 0x5D];
    private static readonly HashSet<int> Modifiers = [0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C];
    private readonly HashSet<int> physical = [];
    private readonly HashSet<int> draining = [];
    private readonly Dictionary<int, Press> active = [];
    private bool enabled, compatibility;

    public IReadOnlyList<Stroke> Configure(bool value, bool chords) {
        var output = ReleaseAll(); enabled = value; compatibility = chords; return output;
    }
    public void SeedModifier(int key) { if (Modifiers.Contains(key)) physical.Add(key); }
    public Decision Process(KeyEvent input) {
        if (input.Injected) return Decision.Pass;
        if (Modifiers.Contains(input.Key)) {
            if (input.Down) physical.Add(input.Key); else physical.Remove(input.Key);
            if (active.Values.Any(p => p.Removed.Contains(input.Key) || (!input.Down && p.Added.Contains(input.Key))))
                return new(true, Array.Empty<Stroke>());
            return Decision.Pass;
        }
        if (draining.Contains(input.Key)) {
            if (!input.Down) draining.Remove(input.Key);
            return new(true, Array.Empty<Stroke>());
        }
        if (active.TryGetValue(input.Key, out var held)) {
            if (input.Down) return new(true, new[] { new Stroke(held.Target, true) });
            active.Remove(input.Key);
            return new(true, End(held).ToArray());
        }
        if (!enabled || !input.Down) return Decision.Pass;
        var press = Map(input.Key);
        if (press is null) return Decision.Pass;
        active.Add(input.Key, press);
        var strokes = press.Removed.Select(k => new Stroke(k, false))
            .Concat(press.Added.Select(k => new Stroke(k, true)))
            .Append(new Stroke(press.Target, true)).ToArray();
        return new(true, strokes);
    }
    private Press? Map(int key) {
        int index = Array.IndexOf(Media, key);
        if (index >= 0) return new(0x70 + index, [], []);
        if (key >= 0x70 && key <= 0x78) return new(Media[key - 0x70], [], []);
        if (!compatibility) return null;
        if (key == 0x84) return new(0x79, [], new[] { 0x5B, 0xA0 }.Where(physical.Contains).ToArray());
        if (key == 0x09 && physical.Contains(0xA2) && physical.Contains(0xA4)) return new(0x7A, [], [0xA2, 0xA4]);
        if (key == 0x79) return new(0x84, new[] { 0xA0, 0x5B }.Where(k => !physical.Contains(k)).ToArray(), []);
        if (key == 0x7A) return new(0x09, new[] { 0xA2, 0xA4 }.Where(k => !physical.Contains(k)).ToArray(), []);
        return null;
    }
    private IEnumerable<Stroke> End(Press press) {
        yield return new(press.Target, false);
        foreach (int key in press.Added.Reverse())
            if (!physical.Contains(key) && !active.Values.Any(p => p.Added.Contains(key))) yield return new(key, false);
        foreach (int key in press.Removed)
            if (physical.Contains(key) && !active.Values.Any(p => p.Removed.Contains(key))) yield return new(key, true);
    }
    public IReadOnlyList<Stroke> ReleaseAll() {
        var result = new List<Stroke>();
        foreach (var key in active.Keys.ToArray()) {
            var press = active[key]; active.Remove(key); draining.Add(key);
            result.AddRange(End(press));
        }
        return result;
    }
}
