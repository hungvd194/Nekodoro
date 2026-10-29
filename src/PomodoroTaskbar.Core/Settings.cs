namespace PomodoroTaskbar.Core;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;
    public PomodoroSettings Pomodoro { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    public ColorSettings Colors { get; set; } = new();
    public bool NotificationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool LaunchAtStartup { get; set; }
    public void Validate()
    {
        if (SchemaVersion != 2) throw new InvalidDataException("Unsupported settings schema.");
        Pomodoro ??= new(); Overlay ??= new(); Colors ??= new();
        Pomodoro.Validate(); Overlay.Validate(); Colors.Validate();
    }
}

public sealed class PomodoroSettings
{
    public double FocusMinutes { get; set; } = 25;
    public double ShortBreakMinutes { get; set; } = 5;
    public double LongBreakMinutes { get; set; } = 15;
    public int LongBreakEvery { get; set; } = 4;
    public bool AutoStartNext { get; set; }
    public void Validate()
    {
        if (!double.IsFinite(FocusMinutes) || FocusMinutes <= 0 || FocusMinutes > 1440 ||
            !double.IsFinite(ShortBreakMinutes) || ShortBreakMinutes <= 0 || ShortBreakMinutes > 1440 ||
            !double.IsFinite(LongBreakMinutes) || LongBreakMinutes <= 0 || LongBreakMinutes > 1440 ||
            LongBreakEvery is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(PomodoroSettings));
    }
}

public sealed class OverlaySettings
{
    public string Edge { get; set; } = "Top";
    public double LengthPercent { get; set; } = 60;
    public double ThicknessDip { get; set; } = 4;
    public string Alignment { get; set; } = "Center";
    public double EdgeOffsetDip { get; set; }
    public string DrainDirection { get; set; } = "FromEnd";
    public double Opacity { get; set; } = 1;
    public bool ShowRemainingTime { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool HideOnFullscreen { get; set; } = true;
    public string? MonitorId { get; set; }
    public void Validate()
    {
        if (!new[] { "Top", "Bottom", "Left", "Right" }.Contains(Edge) ||
            !new[] { "Start", "Center", "End" }.Contains(Alignment) ||
            !new[] { "FromStart", "FromEnd", "ToCenter" }.Contains(DrainDirection) ||
            !InRange(LengthPercent, 10, 100) || !InRange(ThicknessDip, 2, 20) ||
            !InRange(EdgeOffsetDip, 0, 30) || !InRange(Opacity, 0, 1))
            throw new ArgumentOutOfRangeException(nameof(OverlaySettings));
    }
    private static bool InRange(double n, double min, double max) => double.IsFinite(n) && n >= min && n <= max;
}

public sealed class ColorSettings
{
    public string FocusColor { get; set; } = "#BF6559";
    public string ShortBreakColor { get; set; } = "#7F9276";
    public string LongBreakColor { get; set; } = "#97B4BE";
    public void Validate()
    {
        if (!ValidColor(FocusColor) || !ValidColor(ShortBreakColor) || !ValidColor(LongBreakColor))
            throw new ArgumentOutOfRangeException(nameof(ColorSettings));
    }
    private static bool ValidColor(string? value) => value is { Length: 7 or 9 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}
