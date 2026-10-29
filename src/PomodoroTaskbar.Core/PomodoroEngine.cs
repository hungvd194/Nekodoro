using System.Diagnostics;

namespace PomodoroTaskbar.Core;

public enum SessionKind { Focus, ShortBreak, LongBreak }
public enum TimerState { Idle, Running, Paused, AwaitingNext }
public interface IClock { TimeSpan Elapsed { get; } }
public sealed class MonotonicClock : IClock
{
    private readonly Stopwatch watch = Stopwatch.StartNew();
    public TimeSpan Elapsed => watch.Elapsed;
}
public sealed record SessionSnapshot(SessionKind Kind, TimerState State, TimeSpan Duration, TimeSpan Remaining, int CompletedFocusSessions);

public sealed class PomodoroEngine
{
    private PomodoroSettings settings;
    private readonly IClock clock;
    private TimeSpan anchor;
    private TimeSpan remaining;
    public SessionKind Kind { get; private set; } = SessionKind.Focus;
    public TimerState State { get; private set; } = TimerState.Idle;
    public TimeSpan Duration { get; private set; }
    public int CompletedFocusSessions { get; private set; }
    public TimeSpan Remaining => State == TimerState.Running ? MaxZero(remaining - (clock.Elapsed - anchor)) : remaining;
    public double Progress => Duration > TimeSpan.Zero ? Math.Clamp(Remaining.TotalSeconds / Duration.TotalSeconds, 0, 1) : 0;
    public event Action? SessionCompleted;

    public PomodoroEngine(PomodoroSettings settings, IClock? clock = null)
    {
        settings.Validate(); this.settings = Copy(settings); this.clock = clock ?? new MonotonicClock();
        Duration = remaining = GetDuration(Kind);
    }
    public void UpdateSettings(PomodoroSettings value) { value.Validate(); settings = Copy(value); }
    public void Start()
    {
        if (State == TimerState.Running) return;
        if (State == TimerState.Paused) { Resume(); return; }
        Duration = remaining = GetDuration(Kind); anchor = clock.Elapsed; State = TimerState.Running;
    }
    public void Pause()
    {
        if (State != TimerState.Running) return;
        Tick();
        if (State != TimerState.Running) return;
        remaining = Remaining; State = TimerState.Paused;
    }
    public void Resume() { if (State == TimerState.Paused) { anchor = clock.Elapsed; State = TimerState.Running; } }
    public void Reset() { Duration = remaining = GetDuration(Kind); State = TimerState.Idle; }
    public void Skip() { MoveNext(); }
    public void Tick()
    {
        if (State != TimerState.Running || Remaining > TimeSpan.Zero) return;
        if (Kind == SessionKind.Focus) CompletedFocusSessions++;
        MoveNext();
        SessionCompleted?.Invoke();
    }
    private void MoveNext()
    {
        Kind = Kind == SessionKind.Focus
            ? (CompletedFocusSessions > 0 && CompletedFocusSessions % settings.LongBreakEvery == 0 ? SessionKind.LongBreak : SessionKind.ShortBreak)
            : SessionKind.Focus;
        Duration = remaining = GetDuration(Kind);
        anchor = clock.Elapsed;
        State = settings.AutoStartNext ? TimerState.Running : TimerState.AwaitingNext;
    }
    public SessionSnapshot Snapshot() => new(Kind, State, Duration, Remaining, CompletedFocusSessions);
    public void Restore(SessionSnapshot value)
    {
        if (!Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.State) || value.Duration <= TimeSpan.Zero ||
            value.Duration > TimeSpan.FromDays(1) || value.Remaining < TimeSpan.Zero || value.Remaining > value.Duration ||
            value.CompletedFocusSessions < 0) throw new InvalidDataException("Invalid session snapshot.");
        Kind = value.Kind; Duration = value.Duration; remaining = value.Remaining; CompletedFocusSessions = value.CompletedFocusSessions;
        State = value.State == TimerState.Running ? TimerState.Paused : value.State;
        anchor = clock.Elapsed;
    }
    private TimeSpan GetDuration(SessionKind kind) => TimeSpan.FromMinutes(kind switch
    { SessionKind.Focus => settings.FocusMinutes, SessionKind.ShortBreak => settings.ShortBreakMinutes, _ => settings.LongBreakMinutes });
    private static TimeSpan MaxZero(TimeSpan value) => value < TimeSpan.Zero ? TimeSpan.Zero : value;
    private static PomodoroSettings Copy(PomodoroSettings s) => new() { FocusMinutes = s.FocusMinutes, ShortBreakMinutes = s.ShortBreakMinutes, LongBreakMinutes = s.LongBreakMinutes, LongBreakEvery = s.LongBreakEvery, AutoStartNext = s.AutoStartNext };
}
