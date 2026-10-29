using PomodoroTaskbar.Core;

var checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
Check(System.Text.Json.JsonSerializer.Deserialize<AppSettings>("{\"Overlay\":{}}")!.Overlay.AlwaysOnTop, "Existing settings default to always-on-top");
var topmostSettings = new AppSettings(); topmostSettings.Overlay.AlwaysOnTop = false;
Check(!System.Text.Json.JsonSerializer.Deserialize<AppSettings>(System.Text.Json.JsonSerializer.Serialize(topmostSettings))!.Overlay.AlwaysOnTop, "Always-on-top opt-out survives serialization");
var clock = new FakeClock();
var engine = new PomodoroEngine(new() { FocusMinutes = 1, ShortBreakMinutes = 1, LongBreakMinutes = 1 }, clock);
var completed = 0;
engine.SessionCompleted += () => completed++;
engine.Start(); clock.Advance(20); engine.Pause(); clock.Advance(200);
Check(engine.Remaining == TimeSpan.FromSeconds(40), "Pause freezes elapsed time");
engine.Resume(); clock.Advance(40); engine.Tick(); engine.Tick();
Check(completed == 1 && engine.Kind == SessionKind.ShortBreak && engine.CompletedFocusSessions == 1, "Complete exactly once");
Check(engine.State == TimerState.AwaitingNext, "Manual next default");
engine.Skip(); engine.Start(); engine.Skip();
Check(engine.CompletedFocusSessions == 1, "Skip does not complete focus");
engine.Skip(); engine.Start(); engine.Reset();
Check(engine.State == TimerState.Idle && engine.CompletedFocusSessions == 1, "Reset preserves count");
for (var i = 0; i < 3; i++) { engine.Start(); clock.Advance(60); engine.Tick(); if (i < 2) engine.Skip(); }
Check(engine.Kind == SessionKind.LongBreak && engine.CompletedFocusSessions == 4, "Long break after fourth focus");
engine.Skip(); engine.Start(); clock.Advance(10);
var restored = new PomodoroEngine(new()); restored.Restore(engine.Snapshot());
Check(restored.State == TimerState.Paused && restored.Remaining == TimeSpan.FromSeconds(50), "Restore pauses running session");
engine.UpdateSettings(new() { FocusMinutes = 2 });
Check(engine.Duration == TimeSpan.FromMinutes(1), "Settings do not alter active duration");
engine.Reset(); Check(engine.Duration == TimeSpan.FromMinutes(2), "New duration used on reset");
var automatic = new PomodoroEngine(new() { FocusMinutes = 1, AutoStartNext = true }, clock);
automatic.Start(); clock.Advance(60); automatic.Tick();
Check(automatic.State == TimerState.Running && automatic.Kind == SessionKind.ShortBreak, "Auto starts next");
clock.Advance(600); automatic.Pause();
Check(automatic.State == TimerState.Paused && automatic.Kind == SessionKind.Focus && automatic.Remaining == automatic.Duration,
    "Pause at completion also pauses automatically started next session");
var delayed = new PomodoroEngine(new() { FocusMinutes = 1 }, clock);
delayed.Start(); clock.Advance(3600);
Check(delayed.Remaining == TimeSpan.Zero && delayed.Progress == 0, "Delayed tick clamps remaining time");
delayed.Tick(); delayed.Tick();
Check(delayed.CompletedFocusSessions == 1 && delayed.State == TimerState.AwaitingNext,
    "Delayed tick completes only the active session");
var beforeInvalidRestore = delayed.Snapshot();
var rejected = false;
try { delayed.Restore(new(SessionKind.Focus, TimerState.Running, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(11), 0)); }
catch (InvalidDataException) { rejected = true; }
Check(rejected && delayed.Snapshot() == beforeInvalidRestore, "Invalid snapshot rejected without mutating engine");
rejected = false;
try { delayed.UpdateSettings(new() { FocusMinutes = double.NaN }); }
catch (ArgumentOutOfRangeException) { rejected = true; }
delayed.Skip(); delayed.Start();
Check(rejected && delayed.Duration == TimeSpan.FromMinutes(1), "Invalid settings do not replace active configuration");
rejected = false;
try { new AppSettings { Overlay = new() { LengthPercent = double.NaN } }.Validate(); }
catch (ArgumentOutOfRangeException) { rejected = true; }
Check(rejected, "Reject non-finite geometry");
var path = Path.Combine(Directory.GetCurrentDirectory(), ".test-data", "PomodoroTests-" + Guid.NewGuid());
try
{
    var store = new JsonStore(path); store.SaveSettings(new() { Colors = new() { FocusColor = "#123456", ShortBreakColor = "#ABCDEF", LongBreakColor = "#789ABC" } });
    Check(store.LoadSettings().Colors.FocusColor == "#123456" && store.LoadSettings().Colors.ShortBreakColor == "#ABCDEF" &&
        store.LoadSettings().Colors.LongBreakColor == "#789ABC", "Custom colors round trip");
    var savedJson = File.ReadAllText(Path.Combine(path, "settings.json"));
    Check(savedJson.Contains("\"colors\"") && !savedJson.Contains("theme", StringComparison.OrdinalIgnoreCase) &&
        !savedJson.Contains("animation", StringComparison.OrdinalIgnoreCase), "New settings serialize colors only");
    File.WriteAllText(Path.Combine(path, "settings.json"), """
        {"schemaVersion":1,"pomodoro":{"focusMinutes":42,"shortBreakMinutes":8,"longBreakMinutes":20,"longBreakEvery":3,"autoStartNext":true},
        "overlay":{"edge":"Left","lengthPercent":73,"thicknessDip":9,"alignment":"End","edgeOffsetDip":11,"drainDirection":"ToCenter","opacity":0.7,"showRemainingTime":true,"hideOnFullscreen":false,"monitorId":"DISPLAY2"},
        "theme":{"id":"galaxy","focusColor":"#112233","shortBreakColor":"#445566","longBreakColor":"#778899","animationEnabled":true,"animationSpeed":3},
        "notificationsEnabled":false,"soundEnabled":false,"launchAtStartup":true}
        """);
    var migrated = store.LoadSettings();
    Check(migrated.SchemaVersion == 2 && migrated.Colors.FocusColor == "#112233" && migrated.Colors.ShortBreakColor == "#445566" &&
        migrated.Colors.LongBreakColor == "#778899", "Legacy animated selection preserves all custom colors");
    Check(migrated.Pomodoro.FocusMinutes == 42 && migrated.Pomodoro.ShortBreakMinutes == 8 && migrated.Pomodoro.LongBreakMinutes == 20 &&
        migrated.Pomodoro.LongBreakEvery == 3 && migrated.Pomodoro.AutoStartNext, "Migration preserves timer preferences");
    Check(migrated.Overlay.Edge == "Left" && migrated.Overlay.LengthPercent == 73 && migrated.Overlay.ThicknessDip == 9 &&
        migrated.Overlay.Alignment == "End" && migrated.Overlay.EdgeOffsetDip == 11 && migrated.Overlay.DrainDirection == "ToCenter" &&
        migrated.Overlay.Opacity == 0.7 && migrated.Overlay.ShowRemainingTime && !migrated.Overlay.HideOnFullscreen && migrated.Overlay.MonitorId == "DISPLAY2" &&
        !migrated.NotificationsEnabled && !migrated.SoundEnabled && migrated.LaunchAtStartup, "Migration preserves overlay and app preferences");
    store.SaveSettings(migrated);
    Check(!File.ReadAllText(Path.Combine(path, "settings.json")).Contains("theme", StringComparison.OrdinalIgnoreCase), "Migrated save removes obsolete configuration");
    store.SaveSession(engine.Snapshot()); Check(store.LoadSession()?.Kind == engine.Kind, "Session round trip");
    File.WriteAllText(Path.Combine(path, "settings.json"), "{broken");
    Check(store.LoadSettings().Pomodoro.FocusMinutes == 25 && Directory.GetFiles(path, "*.invalid-*").Length == 1, "Corrupt settings fallback and backup");
    File.WriteAllText(Path.Combine(path, "settings.json"), "{\"schemaVersion\":99}");
    Check(store.LoadSettings().SchemaVersion == 2, "Unsupported schema fallback");
    File.WriteAllText(Path.Combine(path, "settings.json"), "{\"schemaVersion\":\"bad\"}");
    Check(store.LoadSettings().SchemaVersion == 2, "Malformed schema fallback");
}
finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
Console.WriteLine($"PASS: {checks} core checks");

sealed class FakeClock : IClock
{
    public TimeSpan Elapsed { get; private set; }
    public void Advance(double seconds) => Elapsed += TimeSpan.FromSeconds(seconds);
}
