using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PomodoroTaskbar.Core;
using Forms = System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace PomodoroTaskbar.Windows;

internal sealed class SettingsWindow : Window
{
    private readonly AppSettings draft;
    private readonly Action<AppSettings> preview;
    private readonly List<Action> validate = [], repaint = [];
    private readonly HashSet<TextBox> invalid = [];
    private readonly Dictionary<string, ComboBox> combos = [];
    private readonly List<Border> pages = [];
    private readonly List<Button> navigation = [];
    private readonly ContentControl pageHost = new();
    private readonly TextBlock dirty = Text("", 12, "#A94F44");
    private readonly TextBlock feedback = Text("Thanh ngoài màn hình được xem trước ngay.", 12);
    private StackPanel panel = new();
    private string committed;
    internal event Action<bool>? EditPosition;

    internal SettingsWindow(AppSettings settings, Action<AppSettings> preview, Action<AppSettings> save, Action<string> timerCommand)
    {
        this.preview = preview;
        draft = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
        committed = JsonSerializer.Serialize(draft);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Nekodoro;component/SettingsStyles.xaml", UriKind.Relative) });
        Title = "Nekodoro · Cài đặt"; Icon = AppIcon.WindowImage();
        var area = SystemParameters.WorkArea;
        Width = Math.Min(760, area.Width - 24); Height = Math.Min(760, area.Height - 24);
        MinWidth = Math.Min(580, Width); MinHeight = Math.Min(480, Height);
        MaxWidth = area.Width; MaxHeight = area.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        Foreground = Brush("#403D37"); Background = Brush("#F7F5EE"); UseLayoutRounding = true;
        var layout = new Grid { Margin = new Thickness(24, 20, 24, 20) };
        for (int i = 0; i < 4; i++) layout.RowDefinitions.Add(new RowDefinition { Height = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
        Content = layout;
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 20) };
        var cat = new System.Windows.Controls.Image { Source = AppIcon.WindowImage(), Width = 46, Height = 46, Margin = new Thickness(12, 0, 0, 0), IsHitTestVisible = false };
        DockPanel.SetDock(cat, Dock.Right); header.Children.Add(cat);
        var heading = new StackPanel(); heading.Children.Add(Text("Cài đặt", 26, "#403D37", FontWeights.SemiBold));
        heading.Children.Add(Text("Nekodoro · Một nhịp nhỏ, một ngày nhẹ hơn.", 12)); header.Children.Add(heading); layout.Children.Add(header);
        var nav = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 12) };
        string[] names = ["Nhịp", "Thanh", "Màu", "Ứng dụng"];
        for (int i = 0; i < names.Length; i++)
        {
            int index = i; var button = MakeButton(names[i], () => SelectPage(index));
            button.Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 3 ? 0 : 4, 0); AutomationProperties.SetName(button, names[i]); navigation.Add(button); nav.Children.Add(button);
        }
        Grid.SetRow(nav, 1); layout.Children.Add(nav); Grid.SetRow(pageHost, 2); layout.Children.Add(pageHost);

        NewPage("Nhịp của bạn", "Chọn khoảng tập trung vừa sức. Áp dụng từ phiên tiếp theo.");
        panel.Children.Add(Columns(
            Number("Tập trung · phút", draft.Pomodoro.FocusMinutes, 1, 240, x => draft.Pomodoro.FocusMinutes = x),
            Number("Nghỉ ngắn · phút", draft.Pomodoro.ShortBreakMinutes, 1, 120, x => draft.Pomodoro.ShortBreakMinutes = x),
            Number("Nghỉ dài · phút", draft.Pomodoro.LongBreakMinutes, 1, 240, x => draft.Pomodoro.LongBreakMinutes = x)));
        panel.Children.Add(Number("Nghỉ dài sau mỗi (phiên)", draft.Pomodoro.LongBreakEvery, 1, 20, x => draft.Pomodoro.LongBreakEvery = (int)x, integer: true));
        Check("Tự bắt đầu phiên tiếp theo", draft.Pomodoro.AutoStartNext, x => draft.Pomodoro.AutoStartNext = x);
        Note("Không cần vội. Bạn có thể tạm dừng bất cứ lúc nào trong cửa sổ tập trung.");

        NewPage("Thanh thời gian", "Chọn một cạnh trên hình xem trước hoặc dùng danh sách bên dưới.");
        panel.Children.Add(ScreenPreview());
        var screens = Forms.Screen.AllScreens;
        string selected = screens.FirstOrDefault(s => s.DeviceName == draft.Overlay.MonitorId)?.DeviceName ?? Forms.Screen.PrimaryScreen!.DeviceName;
        panel.Children.Add(Columns(
            Combo("Màn hình", screens.Select(s => s.DeviceName).ToArray(), selected, x => draft.Overlay.MonitorId = x),
            Combo("Cạnh màn hình", ["Top", "Bottom", "Left", "Right"], draft.Overlay.Edge, x => draft.Overlay.Edge = x)));
        panel.Children.Add(Number("Độ dài · %", draft.Overlay.LengthPercent, 10, 100, x => draft.Overlay.LengthPercent = x, slider: true));
        panel.Children.Add(Number("Độ dày · DIP", draft.Overlay.ThicknessDip, 2, 20, x => draft.Overlay.ThicknessDip = x, slider: true));
        panel.Children.Add(Number("Độ hiển thị · %", draft.Overlay.Opacity * 100, 0, 100, x => draft.Overlay.Opacity = x / 100, slider: true));
        panel.Children.Add(Columns(
            Combo("Căn vị trí", ["Start", "Center", "End"], draft.Overlay.Alignment, x => draft.Overlay.Alignment = x),
            Number("Khoảng cách mép · DIP", draft.Overlay.EdgeOffsetDip, 0, 30, x => draft.Overlay.EdgeOffsetDip = x)));
        panel.Children.Add(Combo("Hướng thanh thu ngắn", ["FromEnd", "FromStart", "ToCenter"], draft.Overlay.DrainDirection, x => draft.Overlay.DrainDirection = x));
        Check("Hiện thời gian còn lại trên thanh", draft.Overlay.ShowRemainingTime, x => draft.Overlay.ShowRemainingTime = x);
        Check("Luôn hiển thị trên cùng", draft.Overlay.AlwaysOnTop, x => draft.Overlay.AlwaysOnTop = x);
        Check("Kéo thanh để chọn cạnh / màn hình", false, x => EditPosition?.Invoke(x), false);
        var colorLink = MakeButton("Chỉnh màu thanh →", () => SelectPage(2)); colorLink.HorizontalAlignment = HorizontalAlignment.Left; panel.Children.Add(colorLink);

        NewPage("Màu của bạn", "Ba màu đơn sắc cho ba khoảng thời gian trong ngày.");
        panel.Children.Add(Columns(
            ColorField("Tập trung", draft.Colors.FocusColor, x => draft.Colors.FocusColor = x),
            ColorField("Nghỉ ngắn", draft.Colors.ShortBreakColor, x => draft.Colors.ShortBreakColor = x),
            ColorField("Nghỉ dài", draft.Colors.LongBreakColor, x => draft.Colors.LongBreakColor = x)));
        Subheading("Xem trước các phiên");
        string[] states = ["Tập trung", "Nghỉ ngắn", "Nghỉ dài"];
        for (int i = 0; i < states.Length; i++)
        {
            int state = i; var row = new StackPanel { Margin = new Thickness(0, 0, 0, 18) }; row.Children.Add(Text(states[i], 13, "#403D37"));
            var sample = new Border { Height = 8, CornerRadius = new CornerRadius(4), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Width = 320 - 60 * i };
            var track = new Grid { Height = 18, HorizontalAlignment = HorizontalAlignment.Left, Width = 360, Margin = new Thickness(0, 6, 0, 0) };
            track.Children.Add(new Border { Height = 8, Background = Brush("#ECE9E1"), CornerRadius = new CornerRadius(4) }); track.Children.Add(sample); row.Children.Add(track); panel.Children.Add(row);
            repaint.Add(() => sample.Background = Brush(state == 0 ? draft.Colors.FocusColor : state == 1 ? draft.Colors.ShortBreakColor : draft.Colors.LongBreakColor));
        }
        Note("Màu chỉ thay đổi cách hiển thị, không thay đổi bộ đếm hay phiên đang chạy.");

        NewPage("Hòa vào ngày của bạn", "Những lựa chọn nhỏ để Nekodoro ở bên vừa đủ.");
        Subheading("Khi hết phiên");
        Check("Hiện thông báo", draft.NotificationsEnabled, x => draft.NotificationsEnabled = x);
        Check("Phát âm báo nhẹ", draft.SoundEnabled, x => draft.SoundEnabled = x);
        Subheading("Khi làm việc");
        Check("Ẩn thanh khi ứng dụng khác toàn màn hình", draft.Overlay.HideOnFullscreen, x => draft.Overlay.HideOnFullscreen = x);
        Check("Khởi động cùng Windows", draft.LaunchAtStartup, x => draft.LaunchAtStartup = x);
        Note("Đóng cửa sổ để về khay hệ thống. Nhấp trái biểu tượng mèo để mở cửa sổ tập trung; nhấp phải để mở cài đặt hoặc thoát.");

        var footer = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var messages = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        messages.Children.Add(dirty); feedback.TextWrapping = TextWrapping.Wrap; messages.Children.Add(feedback); footer.Children.Add(messages);
        var actions = new StackPanel { Orientation = Orientation.Horizontal }; actions.Children.Add(MakeButton("Hủy", Close));
        var apply = MakeButton("Lưu thay đổi", () =>
        {
            try
            {
                foreach (var check in validate) check(); draft.Validate(); save(draft); committed = JsonSerializer.Serialize(draft); UpdateDirty();
                feedback.Text = "Đã lưu thay đổi."; feedback.Foreground = Brush("#52664A");
            }
            catch (Exception ex) { feedback.Text = ex.Message; feedback.Foreground = Brush("#A94F44"); }
        });
        apply.Style = (Style)FindResource("Primary"); apply.IsDefault = true; actions.Children.Add(apply);
        Grid.SetColumn(actions, 1); footer.Children.Add(actions); Grid.SetRow(footer, 3); layout.Children.Add(footer);
        Closed += (_, _) => EditPosition?.Invoke(false); SelectPage(0); foreach (var update in repaint) update();
    }

    // The timer has its own FocusWindow. Keep compatibility without periodic settings work.
    internal void UpdateStatus(PomodoroEngine engine) { }
    internal void SetPosition(string edge, string monitor)
    {
        combos["Cạnh màn hình"].SelectedValue = edge;
        var box = combos["Màn hình"];
        if (!box.Items.Cast<Choice>().Any(c => c.Value == monitor)) box.ItemsSource = Forms.Screen.AllScreens.Select(s => new Choice(s.DeviceName, s.DeviceName)).ToArray();
        box.SelectedValue = monitor;
    }
    private void Changed()
    {
        UpdateDirty(); foreach (var update in repaint) update(); preview(draft);
        feedback.Text = "Thanh ngoài màn hình được xem trước ngay."; feedback.Foreground = Brush("#736D63");
    }
    private void UpdateDirty() => dirty.Text = invalid.Count > 0 || JsonSerializer.Serialize(draft) != committed ? "● Chưa lưu" : "";
    private void SelectPage(int index)
    {
        pageHost.Content = pages[index];
        for (int i = 0; i < navigation.Count; i++)
        {
            bool selected = i == index; navigation[i].Background = Brush(selected ? "#FFFFFF" : "#EFEEE7"); navigation[i].BorderBrush = Brush(selected ? "#7F9276" : "#DED9CD");
            navigation[i].BorderThickness = new Thickness(selected ? 2 : 1); navigation[i].FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }
    private void NewPage(string title, string description)
    {
        panel = new StackPanel { Margin = new Thickness(22, 18, 22, 22) };
        pages.Add(new Border { Background = Brush("#FFFFFF"), CornerRadius = new CornerRadius(18), BorderBrush = Brush("#E5E1D8"), BorderThickness = new Thickness(1), Child = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } });
        panel.Children.Add(Text(title, 20, "#403D37", FontWeights.SemiBold)); var hint = Text(description, 13); hint.Margin = new Thickness(0, 5, 0, 20); hint.TextWrapping = TextWrapping.Wrap; panel.Children.Add(hint);
    }
    private void Subheading(string title) { var text = Text(title, 15, "#403D37", FontWeights.SemiBold); text.Margin = new Thickness(0, 12, 0, 10); panel.Children.Add(text); }
    private void Note(string text) { var note = Text(text, 13); note.TextWrapping = TextWrapping.Wrap; note.Margin = new Thickness(0, 16, 0, 0); panel.Children.Add(note); }
    private FrameworkElement Number(string title, double value, double min, double max, Action<double> set, bool integer = false, bool slider = false)
    {
        var field = Field(title); var row = new Grid(); var box = new TextBox { Text = value.ToString(CultureInfo.CurrentCulture), TextAlignment = TextAlignment.Right };
        AutomationProperties.SetName(box, title); var error = Error(); int page = pages.Count - 1; System.Windows.Controls.Slider? range = null; bool updating = false;
        if (slider)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
            range = new System.Windows.Controls.Slider { Minimum = min, Maximum = max, Value = value, TickFrequency = title.Contains("DIP") ? 1 : 5, MinHeight = 40, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            AutomationProperties.SetName(range, title + " · thanh trượt"); row.Children.Add(range); Grid.SetColumn(box, 1);
            range.ValueChanged += (_, _) => { if (!updating) box.Text = Math.Round(range.Value, 1).ToString(CultureInfo.CurrentCulture); };
        }
        row.Children.Add(box); field.Children.Add(row); field.Children.Add(error);
        bool Read()
        {
            if (!double.TryParse(box.Text, out var number) || !double.IsFinite(number) || number < min || number > max || (integer && number != Math.Truncate(number))) { SetError(box, error, $"Nhập {(integer ? "số nguyên " : "")}từ {min} đến {max}."); return false; }
            ClearError(box, error); set(number); if (range is not null) { updating = true; range.Value = number; updating = false; } return true;
        }
        validate.Add(() => { if (!Read()) { SelectPage(page); pageHost.UpdateLayout(); box.BringIntoView(); box.Focus(); throw new ArgumentException($"Kiểm tra trường {title}."); } });
        box.TextChanged += (_, _) => { if (Read()) Changed(); else UpdateDirty(); }; return field;
    }
    private FrameworkElement Combo(string title, string[] values, string selected, Action<string> set)
    {
        var field = Field(title); var box = new ComboBox { ItemsSource = values.Select(v => new Choice(v, Label(v))).ToArray(), DisplayMemberPath = nameof(Choice.Label), SelectedValuePath = nameof(Choice.Value), SelectedValue = selected };
        AutomationProperties.SetName(box, title); field.Children.Add(box); combos[title] = box;
        box.SelectionChanged += (_, _) => { if (box.SelectedValue is string value) { set(value); Changed(); } }; return field;
    }
    private void Check(string title, bool value, Action<bool> set, bool changesSettings = true)
    {
        var box = new CheckBox { Content = new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap }, IsChecked = value, MinHeight = 40, Margin = new Thickness(0, 4, 0, 4) };
        AutomationProperties.SetName(box, title); panel.Children.Add(box); box.Click += (_, _) => { set(box.IsChecked == true); if (changesSettings) Changed(); };
    }
    private FrameworkElement ColorField(string title, string value, Action<string> set)
    {
        var field = Field(title); var swatch = new Border { Background = Brush(value), Height = 12, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 10) }; field.Children.Add(swatch);
        var hex = new TextBox { Text = value.ToUpperInvariant(), MaxLength = 9 }; AutomationProperties.SetName(hex, "Mã màu " + title); field.Children.Add(hex); var error = Error(); field.Children.Add(error); int page = pages.Count - 1;
        bool Read()
        {
            string text = hex.Text.Trim();
            if (text.Length is not (7 or 9) || text[0] != '#' || !text.Skip(1).All(Uri.IsHexDigit)) { SetError(hex, error, "Dùng #RRGGBB hoặc #AARRGGBB."); return false; }
            ClearError(hex, error); value = text.ToUpperInvariant(); set(value); swatch.Background = Brush(value); return true;
        }
        validate.Add(() => { if (!Read()) { SelectPage(page); pageHost.UpdateLayout(); hex.BringIntoView(); hex.Focus(); throw new ArgumentException($"Kiểm tra màu {title}."); } });
        hex.TextChanged += (_, _) => { if (Read()) Changed(); else UpdateDirty(); };
        var picker = MakeButton("Chọn màu…", () =>
        {
            var current = (Color)ColorConverter.ConvertFromString(value);
            using var dialog = new Forms.ColorDialog { FullOpen = true, Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B) };
            var owner = new Forms.NativeWindow(); owner.AssignHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            try { if (dialog.ShowDialog(owner) == Forms.DialogResult.OK) hex.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"; } finally { owner.ReleaseHandle(); }
        });
        picker.Margin = new Thickness(0, 6, 0, 0); AutomationProperties.SetName(picker, "Đổi màu " + title); field.Children.Add(picker); return field;
    }
    private FrameworkElement ScreenPreview()
    {
        var group = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 16) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(172) });
        var screen = new Canvas { Width = 230, Height = 100, Background = Brush("#F1F3EC"), ClipToBounds = true };
        var caption = Text("Màn hình xem trước", 12); Canvas.SetLeft(caption, 55); Canvas.SetTop(caption, 42); screen.Children.Add(caption);
        var bar = new Border { Background = Brush(draft.Colors.FocusColor) }; screen.Children.Add(bar);
        var frame = new Border { Child = screen, CornerRadius = new CornerRadius(6), BorderBrush = Brush("#736D63"), BorderThickness = new Thickness(5), VerticalAlignment = VerticalAlignment.Center }; grid.Children.Add(frame);
        var edgeGrid = new UniformGrid { Columns = 2, Rows = 2, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(edgeGrid, 1); grid.Children.Add(edgeGrid);
        var edges = new Dictionary<string, Button>();
        foreach (var item in new[] { ("Top", "Trên"), ("Bottom", "Dưới"), ("Left", "Trái"), ("Right", "Phải") })
        {
            string edge = item.Item1; var button = MakeButton(item.Item2, () => combos["Cạnh màn hình"].SelectedValue = edge);
            button.Padding = new Thickness(9, 2, 9, 2); button.Margin = new Thickness(2); AutomationProperties.SetName(button, "Chọn " + Label(edge).ToLowerInvariant());
            edgeGrid.Children.Add(button); edges.Add(edge, button);
        }
        var selected = Text("", 12); selected.HorizontalAlignment = HorizontalAlignment.Center; selected.Margin = new Thickness(0, 8, 0, 0);
        repaint.Add(() =>
        {
            var o = draft.Overlay; bool vertical = o.Edge is "Left" or "Right";
            double length = (vertical ? screen.Height : screen.Width) * o.LengthPercent / 100, thickness = Math.Max(2, o.ThicknessDip / 2), offset = o.EdgeOffsetDip / 2;
            double available = (vertical ? screen.Height : screen.Width) - length, start = o.Alignment == "Start" ? 0 : o.Alignment == "End" ? available : available / 2;
            bar.Width = vertical ? thickness : length; bar.Height = vertical ? length : thickness; bar.Opacity = o.Opacity; bar.Background = Brush(draft.Colors.FocusColor);
            Canvas.SetLeft(bar, vertical ? o.Edge == "Left" ? offset : screen.Width - thickness - offset : start); Canvas.SetTop(bar, vertical ? start : o.Edge == "Top" ? offset : screen.Height - thickness - offset);
            selected.Text = "Đang chọn: " + Label(o.Edge).ToLowerInvariant();
            foreach (var pair in edges) { pair.Value.BorderBrush = Brush(pair.Key == o.Edge ? "#7F9276" : "#D8D3C8"); pair.Value.Background = Brush(pair.Key == o.Edge ? "#EDF1E8" : "#FFFFFF"); pair.Value.FontWeight = pair.Key == o.Edge ? FontWeights.SemiBold : FontWeights.Normal; }
        });
        group.Children.Add(grid); group.Children.Add(selected); return group;
    }
    private void SetError(TextBox box, TextBlock error, string text) { invalid.Add(box); error.Text = text; error.Visibility = Visibility.Visible; box.BorderBrush = Brush("#A94F44"); }
    private void ClearError(TextBox box, TextBlock error) { invalid.Remove(box); error.Visibility = Visibility.Collapsed; box.BorderBrush = Brush("#D8D3C8"); }
    private static TextBlock Error() => new() { Foreground = Brush("#A94F44"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0), Visibility = Visibility.Collapsed };
    private static Grid Columns(params FrameworkElement[] children)
    {
        var grid = new Grid(); for (int i = 0; i < children.Length; i++) { grid.ColumnDefinitions.Add(new ColumnDefinition()); var child = children[i]; child.Margin = new Thickness(i == 0 ? 0 : 7, 0, i == children.Length - 1 ? 0 : 7, 12); Grid.SetColumn(child, i); grid.Children.Add(child); } return grid;
    }
    private static StackPanel Field(string title)
    {
        var field = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        var label = Text(title.Replace(" · DIP", " · px"), 13, "#403D37");
        if (title.Contains("DIP")) field.ToolTip = "Tự co theo tỉ lệ hiển thị Windows; 1 px tương ứng 1 pixel ở mức 100%.";
        label.Margin = new Thickness(0, 0, 0, 7); label.TextWrapping = TextWrapping.Wrap; field.Children.Add(label); return field;
    }
    private static Button MakeButton(string label, Action action) { var button = new Button { Content = label, Margin = new Thickness(5, 0, 0, 0) }; button.Click += (_, _) => action(); return button; }
    private static string Label(string value) => value switch { "Top" => "Cạnh trên", "Bottom" => "Cạnh dưới", "Left" => "Cạnh trái", "Right" => "Cạnh phải", "Start" => "Đầu cạnh", "Center" => "Chính giữa", "End" => "Cuối cạnh", "FromEnd" => "Từ cuối về đầu", "FromStart" => "Từ đầu về cuối", "ToCenter" => "Thu về giữa", _ => value };
    private sealed record Choice(string Value, string Label);
    private static SolidColorBrush Brush(string color) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }
    private static TextBlock Text(string text, double size, string color = "#736D63", FontWeight? weight = null) => new() { Text = text, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal };
}
