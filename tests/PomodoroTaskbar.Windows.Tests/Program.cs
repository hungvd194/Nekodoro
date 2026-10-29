using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using PomodoroTaskbar.Core;
using PomodoroTaskbar.Windows;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--benchmark")) return PerformanceProbe.Run();
        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
        try
        {
            int saves = 0;
            var window = new SettingsWindow(new AppSettings(), _ => { }, _ => saves++, _ => { });
            window.Show(); window.UpdateLayout();
            var save = Descendants<Button>(window).Single(b => b.Content as string == "Lưu thay đổi");
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saves == 1 && window.IsVisible, "Save commits and keeps settings visible");
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saves == 2 && window.IsVisible, "Repeated Save keeps settings reachable");
            var duration = Descendants<TextBox>(window).Single(t => AutomationProperties.GetName(t) == "Tập trung · phút");
            duration.Text = "invalid";
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saves == 2 && window.IsVisible, "Invalid Save does not commit or close");
            duration.Text = "25";
            Check(Descendants<TextBlock>(window).Any(t => t.Text == "● Chưa lưu") == false, "Saved draft is not marked dirty");
            var bar = Descendants<Button>(window).Single(b => b.Content as string == "Thanh");
            bar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); window.UpdateLayout();
            Check(Descendants<TextBlock>(window).Any(t => t.Text == "● Chưa lưu") == false, "Switching tabs does not change settings");
            var edge = Descendants<Button>(window).Single(b => AutomationProperties.GetName(b) == "Chọn cạnh trái");
            edge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(Descendants<TextBlock>(window).Any(t => t.Text == "● Chưa lưu"), "Interactive monitor preview marks modified settings");
            var lengthSlider = Descendants<System.Windows.Controls.Slider>(window).Single(s => AutomationProperties.GetName(s) == "Độ dài · % · thanh trượt");
            lengthSlider.Value = 45;
            Check(Descendants<TextBox>(window).Single(t => AutomationProperties.GetName(t) == "Độ dài · %").Text == "45", "Slider and exact numeric value stay synchronized");
            Descendants<Button>(window).Single(b => b.Content as string == "Màu").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); window.UpdateLayout();
            var colorInput = Descendants<TextBox>(window).Single(t => AutomationProperties.GetName(t) == "Mã màu Tập trung");
            colorInput.Text = "#112233";
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(saves == 3 && !Descendants<TextBlock>(window).Any(t => t.Text == "● Chưa lưu"), "Valid custom color saves and clears dirty indicator");
            var rhythm = Descendants<Button>(window).Single(b => b.Content as string == "Nhịp");
            rhythm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); window.UpdateLayout();
            Check(Descendants<TextBox>(window).Any(t => AutomationProperties.GetName(t) == "Tập trung · phút"), "Settings remain navigable after Save");
            window.Close();
            Check(app.ShutdownMode == ShutdownMode.OnExplicitShutdown, "Closing settings does not request application shutdown");

            string? mainCommand = null; int openedSettings = 0;
            var focusView = new FocusWindow(command => mainCommand = command, () => openedSettings++);
            var focusEngine = new PomodoroEngine(new PomodoroSettings());
            focusView.Show(); focusView.UpdateStatus(focusEngine, 4); focusView.UpdateLayout();
            var primary = Descendants<Button>(focusView).Single(b => AutomationProperties.GetName(b) == "Điều khiển phiên");
            primary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(mainCommand == "Bắt đầu / Tiếp tục", "Focus primary starts a session");
            focusEngine.Start(); focusView.UpdateStatus(focusEngine, 4); primary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(mainCommand == "Tạm dừng" && primary.Content as string == "Tạm dừng", "Focus primary pauses a running session");
            focusEngine.Pause(); focusView.UpdateStatus(focusEngine, 4);
            Check(primary.Content as string == "Tiếp tục", "Paused focus view offers Resume");
            focusEngine.Restore(new SessionSnapshot(SessionKind.LongBreak, TimerState.AwaitingNext, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), 4));
            focusView.UpdateStatus(focusEngine, 4);
            Check(Descendants<TextBlock>(focusView).Any(t => t.Text == "Chu kỳ 4/4") && primary.Content as string == "Bắt đầu nghỉ", "Long-break view shows completed cycle and correct next action");
            Descendants<Button>(focusView).Single(b => b.Content as string == "Cài đặt").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(openedSettings == 1, "Focus settings action opens settings");
            focusView.Close();

            // Use a different shell identity so the running user's icon is unaffected.
            using var tray = new TrayIconService(Guid.NewGuid());
            Check(tray.EnsureVisible(), "Windows accepts tray icon registration");
            Check(tray.EnsureVisible(), "Tray re-registration is idempotent");
            tray.Update(new PomodoroEngine(new PomodoroSettings()));
            tray.Dispose();
            Check(!tray.EnsureVisible(), "Disposed tray cannot reappear");

            var geometrySettings = new AppSettings(); geometrySettings.Overlay.HideOnFullscreen = false;
            var geometryEngine = new PomodoroEngine(new PomodoroSettings());
            var cachedOverlay = new OverlayWindow(geometrySettings); cachedOverlay.Show(); cachedOverlay.UpdateLayout();
            cachedOverlay.Render(geometryEngine);
            int placements = cachedOverlay.GeometryUpdateCount, styles = cachedOverlay.ClickThroughUpdateCount;
            cachedOverlay.Render(geometryEngine); cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.GeometryUpdateCount == placements && cachedOverlay.ClickThroughUpdateCount == styles, "Unchanged timer renders do not reposition or rewrite window styles");
            geometrySettings.Overlay.LengthPercent = 40; cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.GeometryUpdateCount == placements + 1, "Mutable settings preview invalidates geometry");
            placements = cachedOverlay.GeometryUpdateCount;
            cachedOverlay.InvalidateLayout(); cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.GeometryUpdateCount == placements, "Stable display invalidation avoids redundant native placement");
            var cover = new Window { Width = 200, Height = 80, WindowStyle = WindowStyle.None, Topmost = true, ShowActivated = false, ShowInTaskbar = false };
            cover.Show();
            var overlayHandle = new System.Windows.Interop.WindowInteropHelper(cachedOverlay).Handle;
            var coverHandle = new System.Windows.Interop.WindowInteropHelper(cover).Handle;
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Native.SetWindowPos(coverHandle, new IntPtr(-1), bounds.Left, bounds.Top, bounds.Width, 80, 0x10);
            Check(Native.GetWindow(overlayHandle, 3) == coverHandle, "Topmost cover reproduces taskbar overlap");
            var foreground = Native.GetForegroundWindow();
            cachedOverlay.Render(geometryEngine);
            Check(Native.GetWindow(coverHandle, 3) == overlayHandle, "Render restores overlay above overlapping topmost window");
            Check(Native.GetForegroundWindow() == foreground && cachedOverlay.GeometryUpdateCount == placements, "Order repair preserves focus and cached geometry");
            Check(!Native.RestoreOverlayOrder(overlayHandle), "Unobscured overlay needs no order repair");
            geometrySettings.Overlay.AlwaysOnTop = false; cachedOverlay.Render(geometryEngine);
            Check(!cachedOverlay.Topmost && Native.GetWindow(coverHandle, 3) != overlayHandle, "Disabling always-on-top leaves cover above overlay");
            geometrySettings.Overlay.AlwaysOnTop = true; cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.Topmost && Native.GetWindow(coverHandle, 3) == overlayHandle, "Enabling always-on-top restores overlay order immediately");
            cover.Close();
            cachedOverlay.Editing = true; cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.ClickThroughUpdateCount == styles + 1, "Editing changes click-through once");
            placements = cachedOverlay.GeometryUpdateCount;
            cachedOverlay.Editing = false; cachedOverlay.Render(geometryEngine);
            Check(cachedOverlay.GeometryUpdateCount == placements + 1 && cachedOverlay.ClickThroughUpdateCount == styles + 2, "Leaving edit mode restores placement and click-through");
            geometrySettings.Overlay.Opacity = 0; cachedOverlay.Render(geometryEngine);
            Check(!cachedOverlay.IsBarVisible, "Transparent overlay is treated as inactive for scheduling");
            cachedOverlay.Close();

            var dataPath = System.IO.Path.Combine(Environment.CurrentDirectory, ".test-data", "Windows-" + Guid.NewGuid());
            using (var controller = new ApplicationController(dataPath, Guid.NewGuid()))
            {
                controller.Start();
                Check(app.Windows.OfType<FocusWindow>().Count(w => w.IsVisible) == 1 && !app.Windows.OfType<SettingsWindow>().Any(w => w.IsVisible), "Startup opens dedicated focus view only");
                controller.ShowSettings();
                var settings = app.Windows.OfType<SettingsWindow>().Single(w => w.IsVisible);
                settings.UpdateLayout();
                var focus = Descendants<TextBox>(settings).Single(t => AutomationProperties.GetName(t) == "Tập trung · phút");
                focus.Text = "30";
                Descendants<Button>(settings).Single(b => b.Content as string == "Lưu thay đổi").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(settings.IsVisible && new JsonStore(dataPath).LoadSettings().Pomodoro.FocusMinutes == 30, "Controller saves without hiding settings");
                focus.Text = "31";
                settings.Close();
                Check(!settings.IsVisible, "Settings close successfully with registered tray");
                controller.ShowSettings();
                var reopened = app.Windows.OfType<SettingsWindow>().Single(w => w.IsVisible);
                reopened.UpdateLayout();
                Check(Descendants<TextBox>(reopened).Single(t => AutomationProperties.GetName(t) == "Tập trung · phút").Text == "30", "Reopening restores committed values rather than unsaved edits");
                reopened.Close();
                controller.ShowSettings();
                Check(app.Windows.OfType<SettingsWindow>().Count(w => w.IsVisible) == 1, "Background controller reopens exactly one settings window");
                foreach (var visible in app.Windows.OfType<FocusWindow>().ToArray()) visible.Close();
                controller.ShowFocus(); controller.ShowFocus();
                Check(app.Windows.OfType<FocusWindow>().Count(w => w.IsVisible) == 1, "Background focus restoration keeps a single focus window");
            }
            Console.WriteLine($"PASS: {checks} Windows regression checks");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { app.Shutdown(); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
