using KeyboardSwitcher;

int count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
var engine = new MappingEngine();
Check(!engine.Process(new(0x70, true)).Suppress, "disabled passthrough");
engine.Configure(true, true);
int[] media = [0xAD, 0xAE, 0xAF, 0xB1, 0xB3, 0xB0, 0xA6, 0xAC, 0x5D];
for (int i = 0; i < media.Length; i++) {
    foreach (var pair in new[] { (media[i], 0x70 + i), (0x70 + i, media[i]) }) {
        var down = engine.Process(new(pair.Item1, true));
        Check(down.Suppress && down.Output.SequenceEqual(new[] { new Stroke(pair.Item2, true) }), $"pair {i} down {pair.Item1}");
        var repeat = engine.Process(new(pair.Item1, true));
        Check(repeat.Output.SequenceEqual(down.Output), $"pair {i} repeat {pair.Item1}");
        var up = engine.Process(new(pair.Item1, false));
        Check(up.Suppress && up.Output.SequenceEqual(new[] { new Stroke(pair.Item2, false) }), $"pair {i} up {pair.Item1}");
    }
}
Check(!engine.Process(new(0xAD, true, true)).Suppress, "injected bypass");
Check(!engine.Process(new(0x41, true)).Suppress, "text untouched");
Check(!engine.Process(new(0x7B, true)).Suppress, "F12 untouched");
Check(!engine.Process(new(0x09, true)).Suppress, "ordinary Tab untouched");
engine.Process(new(0xAD, true));
Check(engine.Configure(false, true).Contains(new Stroke(0x70, false)), "disable releases mapped hold");
Check(engine.Process(new(0xAD, true)).Suppress, "held repeat drained after disable");
Check(engine.Process(new(0xAD, false)).Suppress, "held release drained after disable");
Check(!engine.Process(new(0xAD, true)).Suppress, "new down native after disable");
engine.Configure(true, true);
engine.Process(new(0xA2, true)); engine.Process(new(0xA4, true));
var chord = engine.Process(new(0x09, true));
Check(chord.Output.SequenceEqual(new[] { new Stroke(0xA2, false), new Stroke(0xA4, false), new Stroke(0x7A, true) }), "Ctrl Alt Tab to F11");
var release = engine.Process(new(0x09, false));
Check(release.Output.SequenceEqual(new[] { new Stroke(0x7A, false), new Stroke(0xA2, true), new Stroke(0xA4, true) }), "physical modifiers restored");
engine.Process(new(0xA2, false)); engine.Process(new(0xA4, false));
var reverse = engine.Process(new(0x7A, true));
Check(reverse.Output.SequenceEqual(new[] { new Stroke(0xA2, true), new Stroke(0xA4, true), new Stroke(0x09, true) }), "F11 to Ctrl Alt Tab");
Check(engine.Process(new(0x7A, false)).Output.SequenceEqual(new[] { new Stroke(0x09, false), new Stroke(0xA4, false), new Stroke(0xA2, false) }), "synthetic chord modifiers released");
engine.Process(new(0xA0, true)); engine.Process(new(0x5B, true));
Check(engine.Process(new(0x84, true)).Output.Last() == new Stroke(0x79, true), "F21 chord to F10");
engine.Process(new(0xA0, false)); engine.Process(new(0x5B, false));
Check(engine.Process(new(0x84, false)).Output.Count == 1, "released physical modifiers not restored");
var f10 = engine.Process(new(0x79, true));
Check(f10.Output.SequenceEqual(new[] { new Stroke(0xA0, true), new Stroke(0x5B, true), new Stroke(0x84, true) }), "F10 to Shift Win F21");
Check(engine.ReleaseAll().Count == 3, "shutdown drains chord");
engine.Configure(true, false);
Check(!engine.Process(new(0x7A, true)).Suppress, "compatibility can be disabled");
Console.WriteLine($"{count} assertions passed");
