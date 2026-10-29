using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using PomodoroTaskbar.Core;
using Forms = System.Windows.Forms;

namespace PomodoroTaskbar.Windows;

internal sealed class OverlayWindow : Window
{
    private readonly Grid root = new();
    private readonly Border fill = new();
    private readonly TextBlock label = new() { Foreground = Brushes.White, FontSize = 11, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private string colorKey = "";
    private IntPtr handle;
    private Forms.Screen? screen;
    private LayoutKey? layoutKey;
    private (int X, int Y, int Width, int Height)? nativeBounds;
    private bool layoutDirty = true;
    private bool screenDirty = true;
    private bool editing;
    private bool? clickThrough;
    private bool vertical;
    private double dpiX = 1, dpiY = 1;
    private int fillPixels = -1;
    private long labelSeconds = -1;
    private readonly record struct LayoutKey(string? Monitor, string Edge, double Length, double Thickness, double Offset, string Alignment, string DrainDirection);
    internal AppSettings Settings { get; set; }
    internal bool Editing
    {
        get => editing;
        set
        {
            if (editing == value) return;
            editing = value; nativeBounds = null; InvalidateLayout();
        }
    }
    internal bool IsBarVisible => Visibility == Visibility.Visible && Opacity > 0;
    internal int GeometryUpdateCount { get; private set; }
    internal int ClickThroughUpdateCount { get; private set; }
    internal int RenderCount { get; private set; }
    internal event Action<string, string>? PositionChanged;
    internal OverlayWindow(AppSettings settings)
    {
        Settings = settings;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false;
        ShowActivated = false; Topmost = true;
        root.Background = new SolidColorBrush(Color.FromArgb(75, 20, 23, 35));
        root.ClipToBounds = true;
        root.Children.Add(fill); root.Children.Add(label); Content = root;
        SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(this).Handle;
            UpdateClickThrough(); InvalidateLayout();
        };
        DpiChanged += (_, _) => InvalidateLayout();
        SizeChanged += (_, _) => fillPixels = -1;
        MouseLeftButtonDown += (_, e) =>
        {
            if (!Editing) return;
            DragMove();
            var point = Forms.Cursor.Position;
            var s = Forms.Screen.FromPoint(point); var b = s.Bounds;
            var distances = new[] { (Math.Abs(point.X - b.Left), "Left"), (Math.Abs(point.X - b.Right), "Right"), (Math.Abs(point.Y - b.Top), "Top"), (Math.Abs(point.Y - b.Bottom), "Bottom") };
            PositionChanged?.Invoke(distances.MinBy(x => x.Item1).Item2, s.DeviceName);
        };
    }
    internal void InvalidateLayout()
    {
        layoutDirty = true; screenDirty = true; fillPixels = -1;
    }
    private void UpdateClickThrough()
    {
        if (handle == IntPtr.Zero || clickThrough == !Editing) return;
        Native.ClickThrough(handle, !Editing); clickThrough = !Editing; ClickThroughUpdateCount++;
    }
    internal void Render(PomodoroEngine engine)
    {
        RenderCount++;
        var o = Settings.Overlay;
        if (handle == IntPtr.Zero) return;
        if (Topmost != o.AlwaysOnTop) Topmost = o.AlwaysOnTop;
        var key = new LayoutKey(o.MonitorId, o.Edge, o.LengthPercent, o.ThicknessDip, o.EdgeOffsetDip, o.Alignment, o.DrainDirection);
        if (layoutDirty || layoutKey != key)
        {
            bool refreshScreen = screenDirty || screen is null || layoutKey?.Monitor != key.Monitor;
            layoutDirty = false; screenDirty = false; layoutKey = key;
            if (refreshScreen)
                screen = Forms.Screen.AllScreens.FirstOrDefault(x => x.DeviceName == o.MonitorId) ?? Forms.Screen.PrimaryScreen!;
            var b = screen!.Bounds; vertical = o.Edge is "Left" or "Right";
            // Native coordinates are physical pixels; WPF content and thickness are DPI-independent.
            // Moving across monitors raises DpiChanged and recalculates using the destination scale.
            var dpi = VisualTreeHelper.GetDpi(this); dpiX = dpi.DpiScaleX; dpiY = dpi.DpiScaleY;
            double scale = vertical ? dpiX : dpiY;
            int thickness = Math.Max(2, (int)Math.Round(o.ThicknessDip * scale));
            int offset = (int)Math.Round(o.EdgeOffsetDip * scale);
            int length = (int)Math.Round((vertical ? b.Height : b.Width) * o.LengthPercent / 100);
            int available = (vertical ? b.Height : b.Width) - length;
            int start = o.Alignment == "Start" ? 0 : o.Alignment == "End" ? available : available / 2;
            int x = vertical ? (o.Edge == "Left" ? b.Left + offset : b.Right - thickness - offset) : b.Left + start;
            int y = vertical ? b.Top + start : (o.Edge == "Top" ? b.Top + offset : b.Bottom - thickness - offset);
            var bounds = (x, y, vertical ? thickness : length, vertical ? length : thickness);
            if (!Editing && nativeBounds != bounds)
            {
                nativeBounds = bounds;
                Native.SetWindowPos(handle, new IntPtr(o.AlwaysOnTop ? -1 : -2), bounds.x, bounds.y, bounds.Item3, bounds.Item4, 0x10);
                GeometryUpdateCount++;
            }
            fill.Width = double.NaN; fill.Height = double.NaN; fillPixels = -1;
            fill.HorizontalAlignment = vertical ? System.Windows.HorizontalAlignment.Stretch : o.DrainDirection == "ToCenter" ? System.Windows.HorizontalAlignment.Center : o.DrainDirection == "FromStart" ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
            fill.VerticalAlignment = !vertical ? VerticalAlignment.Stretch : o.DrainDirection == "ToCenter" ? VerticalAlignment.Center : o.DrainDirection == "FromStart" ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        }
        UpdateClickThrough();
        if (Opacity != o.Opacity) Opacity = o.Opacity;
        var visibility = !Editing && o.HideOnFullscreen && Native.Fullscreen(screen!, handle) ? Visibility.Hidden : Visibility.Visible;
        if (Visibility != visibility) Visibility = visibility;
        if (!IsBarVisible) return;
        if (!Editing && o.AlwaysOnTop) Native.RestoreOverlayOrder(handle);
        var progress = Math.Clamp(engine.Progress, 0, 1);
        double axisScale = vertical ? dpiY : dpiX;
        int pixels = (int)Math.Round((vertical ? ActualHeight : ActualWidth) * axisScale * progress);
        if (pixels != fillPixels)
        {
            fillPixels = pixels;
            if (vertical) fill.Height = pixels / axisScale; else fill.Width = pixels / axisScale;
        }
        var t = Settings.Colors;
        string color = engine.Kind == SessionKind.Focus ? t.FocusColor : engine.Kind == SessionKind.ShortBreak ? t.ShortBreakColor : t.LongBreakColor;
        if (colorKey != color)
        {
            colorKey = color;
            try { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); fill.Background = brush; }
            catch { fill.Background = Brushes.CornflowerBlue; }
        }
        var labelVisibility = o.ShowRemainingTime ? Visibility.Visible : Visibility.Collapsed;
        if (label.Visibility != labelVisibility) label.Visibility = labelVisibility;
        if (o.ShowRemainingTime)
        {
            long seconds = (long)Math.Ceiling(engine.Remaining.TotalSeconds);
            if (seconds != labelSeconds)
            {
                labelSeconds = seconds;
                label.Text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }
    }
}

