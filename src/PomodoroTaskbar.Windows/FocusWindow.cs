using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using PomodoroTaskbar.Core;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using FontFamily = System.Windows.Media.FontFamily;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace PomodoroTaskbar.Windows;

internal sealed class FocusWindow : Window
{
    private readonly TextBlock time = Text("25:00", 64, "#403D37", FontWeights.SemiBold);
    private readonly TextBlock kind = Text("Tập trung", 18);
    private readonly TextBlock message = Text("Mình cùng tập trung nhé.", 13, "#716B61");
    private readonly TextBlock cycle = Text("Chu kỳ 0/4", 13, "#716B61");
    private readonly Image mascot = new() { Stretch = Stretch.Uniform, IsHitTestVisible = false };
    private readonly Button primary;
    private readonly WrapPanel paws = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Button reset;
    private TimerState state;
    private (SessionKind Kind, TimerState State, int Seconds, int Completed, int Every)? lastStatus;
    private (int Completed, int Every)? lastCycle;
    private int pose = -1;

    internal FocusWindow(Action<string> timerCommand, Action openSettings)
    {
        Title = "Nekodoro"; Icon = AppIcon.WindowImage();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Nekodoro;component/SettingsStyles.xaml", UriKind.Relative) });
        Width = 440; Height = Math.Min(680, SystemParameters.WorkArea.Height); MinWidth = 380;
        MinHeight = Math.Min(540, SystemParameters.WorkArea.Height); MaxHeight = SystemParameters.WorkArea.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; Background = Brush("#F7F5EE"); Foreground = Brush("#403D37"); UseLayoutRounding = true;
        var layout = new Grid { Margin = new Thickness(26, 18, 26, 18) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 72, MaxHeight = 220 });
        for (int i = 0; i < 4; i++) layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = layout;

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var mark = new Image { Source = AppIcon.WindowImage(), Width = 34, Height = 34, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(mark);
        var brand = new StackPanel(); brand.Children.Add(Text("Nekodoro", 24, "#403D37", FontWeights.SemiBold));
        var tagline = Text("Một nhịp nhỏ, một ngày nhẹ hơn.", 12, "#716B61"); tagline.TextWrapping = TextWrapping.Wrap;
        brand.Children.Add(tagline); header.Children.Add(brand); layout.Children.Add(header);

        mascot.Source = MascotArt.Pose(0); pose = 0;
        AutomationProperties.SetName(mascot, "Mèo Nekodoro sẵn sàng tập trung");
        Grid.SetRow(mascot, 1); layout.Children.Add(mascot);

        var clock = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 14) };
        time.TextAlignment = TextAlignment.Center; Typography.SetNumeralAlignment(time, FontNumeralAlignment.Tabular);
        kind.TextAlignment = TextAlignment.Center; message.TextAlignment = TextAlignment.Center;
        message.Margin = new Thickness(0, 6, 0, 0); message.TextWrapping = TextWrapping.Wrap;
        clock.Children.Add(time); clock.Children.Add(kind); clock.Children.Add(message);
        Grid.SetRow(clock, 2); layout.Children.Add(clock);

        var actions = new StackPanel();
        primary = new Button { Content = "Bắt đầu", Style = (Style)FindResource("Primary"), MinHeight = 46, FontSize = 16, Margin = new Thickness(0, 0, 0, 10) };
        primary.Click += (_, _) => timerCommand(state == TimerState.Running ? "Tạm dừng" : "Bắt đầu / Tiếp tục");
        AutomationProperties.SetName(primary, "Điều khiển phiên"); actions.Children.Add(primary);
        var secondaries = new Grid(); secondaries.ColumnDefinitions.Add(new ColumnDefinition()); secondaries.ColumnDefinitions.Add(new ColumnDefinition());
        var skip = new Button { Content = "Bỏ qua", Margin = new Thickness(0, 0, 5, 0) }; skip.Click += (_, _) => timerCommand("Bỏ qua");
        var settings = new Button { Content = "Cài đặt", Margin = new Thickness(5, 0, 0, 0) }; settings.Click += (_, _) => openSettings();
        Grid.SetColumn(settings, 1); secondaries.Children.Add(skip); secondaries.Children.Add(settings); actions.Children.Add(secondaries);
        Grid.SetRow(actions, 3); layout.Children.Add(actions);

        var progress = new StackPanel { Margin = new Thickness(0, 14, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
        cycle.TextAlignment = TextAlignment.Center; progress.Children.Add(cycle); progress.Children.Add(paws);
        Grid.SetRow(progress, 4); layout.Children.Add(progress);
        reset = new Button { Content = "Đặt lại phiên", Background = Brushes.Transparent, BorderThickness = new Thickness(0), MinHeight = 28, Padding = new Thickness(8, 4, 8, 4), Foreground = Brush("#716B61"), HorizontalAlignment = HorizontalAlignment.Center, FontSize = 12 };
        reset.Click += (_, _) => timerCommand("Đặt lại"); Grid.SetRow(reset, 5); layout.Children.Add(reset);
    }

    internal void UpdateStatus(PomodoroEngine engine, int longBreakEvery)
    {
        int seconds = (int)Math.Ceiling(engine.Remaining.TotalSeconds);
        var key = (engine.Kind, engine.State, seconds, engine.CompletedFocusSessions, longBreakEvery);
        if (lastStatus == key) return;
        lastStatus = key; state = engine.State;
        time.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        kind.Text = engine.Kind switch { SessionKind.Focus => "Tập trung", SessionKind.ShortBreak => "Nghỉ ngắn", _ => "Nghỉ dài" };
        primary.Content = engine.State switch
        {
            TimerState.Running => "Tạm dừng", TimerState.Paused => "Tiếp tục",
            TimerState.AwaitingNext => engine.Kind == SessionKind.Focus ? "Bắt đầu tập trung" : "Bắt đầu nghỉ", _ => "Bắt đầu"
        };
        reset.IsEnabled = engine.State != TimerState.Idle;
        int nextPose = engine.State == TimerState.AwaitingNext ? 5 : engine.State == TimerState.Paused ? 2 : engine.State == TimerState.Idle ? 0 : engine.Kind switch { SessionKind.Focus => 1, SessionKind.ShortBreak => 3, _ => 4 };
        if (pose != nextPose)
        {
            pose = nextPose; mascot.Source = MascotArt.Pose(pose);
            message.Text = pose switch { 1 => "Cứ từng chút một thôi.", 2 => "Mình chờ bạn ở đây.", 3 => "Duỗi vai một chút nhé.", 4 => "Nghỉ lâu hơn một chút nào.", 5 => "Xong một phiên rồi!", _ => "Mình cùng tập trung nhé." };
            AutomationProperties.SetName(mascot, message.Text);
        }
        int every = Math.Max(1, longBreakEvery);
        int completed = engine.CompletedFocusSessions % every;
        if (engine.CompletedFocusSessions > 0 && completed == 0 && engine.Kind == SessionKind.LongBreak) completed = every;
        if (lastCycle != (completed, every))
        {
            lastCycle = (completed, every); cycle.Text = $"Chu kỳ {completed}/{every}"; paws.Children.Clear();
            for (int i = 0; i < every; i++) paws.Children.Add(Paw(i < completed));
        }
    }

    private static FrameworkElement Paw(bool filled)
    {
        var canvas = new Canvas { Width = 24, Height = 27, Margin = new Thickness(4, 5, 4, 0), IsHitTestVisible = false };
        var color = Brush(filled ? "#7F9276" : "#DCD6C9");
        void Dot(double x, double y, double width, double height) { var dot = new Ellipse { Width = width, Height = height, Fill = color }; Canvas.SetLeft(dot, x); Canvas.SetTop(dot, y); canvas.Children.Add(dot); }
        Dot(5, 13, 15, 11); Dot(0, 7, 6, 8); Dot(6, 1, 6, 8); Dot(13, 1, 6, 8); Dot(20, 7, 6, 8);
        return canvas;
    }
    private static SolidColorBrush Brush(string color) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }
    private static TextBlock Text(string text, double size, string color = "#403D37", FontWeight? weight = null) => new() { Text = text, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal };
}
