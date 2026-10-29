using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Win32;
using PomodoroTaskbar.Core;

namespace PomodoroTaskbar.Windows;

internal sealed class ApplicationController : IDisposable
{
    private readonly string directory;
    private readonly JsonStore store;
    private AppSettings settings;
    private readonly PomodoroEngine engine;
    private readonly OverlayWindow overlay;
    private readonly TrayIconService tray;
    private readonly DispatcherTimer timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
    private SettingsWindow? settingsWindow;
    private FocusWindow? focusWindow;
    private SessionKind completingKind;
    private readonly Stopwatch checkpointClock = Stopwatch.StartNew();
    private bool disposed;
    private Task pendingSave = Task.CompletedTask;
    internal ApplicationController(string? dataDirectory = null, Guid? trayIdentity = null)
    {
        directory = dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PomodoroTaskbar");
        store = new JsonStore(directory); settings = store.LoadSettings(); engine = new PomodoroEngine(settings.Pomodoro);
        if (store.LoadSession() is { } session) engine.Restore(session);
        overlay = new OverlayWindow(settings);
        overlay.PositionChanged += (edge, monitor) => settingsWindow?.SetPosition(edge, monitor);
        tray = new TrayIconService(trayIdentity);
        tray.OpenFocusRequested += ShowFocus;
        tray.OpenSettingsRequested += ShowSettings;
        tray.CommandRequested += ExecuteCommand;
        tray.ExitRequested += () => System.Windows.Application.Current.Shutdown();
        engine.SessionCompleted += () =>
        {
            if (settings.NotificationsEnabled)
            {
                try { tray.ShowNotification("Nekodoro · Hoàn tất phiên", $"Đã hoàn tất {KindLabel(completingKind).ToLowerInvariant()}. Phiên tiếp theo: {KindLabel(engine.Kind).ToLowerInvariant()}."); }
                catch (Exception ex) { Log(ex); }
            }
            if (settings.SoundEnabled) { try { System.Media.SystemSounds.Asterisk.Play(); } catch (Exception ex) { Log(ex); } }
            SaveSession();
        };
        timer.Tick += (_, _) =>
        {
            if (engine.State == TimerState.Running) { completingKind = engine.Kind; engine.Tick(); }
            RefreshPresentation();
            if (checkpointClock.Elapsed.TotalSeconds >= 10 && engine.State == TimerState.Running) SaveSession();
        };
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        if (settings.LaunchAtStartup)
        {
            try { ApplyStartup(true); } catch (Exception ex) { Log(ex); }
        }
    }
    internal void Start() { overlay.Show(); tray.EnsureVisible(); RefreshPresentation(); timer.Start(); ShowFocus(); }
    private void RefreshPresentation()
    {
        overlay.Render(engine);
        if (focusWindow?.WindowState != System.Windows.WindowState.Minimized) focusWindow?.UpdateStatus(engine, settings.Pomodoro.LongBreakEvery);
        tray.Update(engine);
        // A hidden/paused bar only needs a light foreground/fullscreen check once per second.
        var interval = engine.State == TimerState.Running && overlay.IsBarVisible
            ? TimeSpan.FromMilliseconds(500) : TimeSpan.FromSeconds(1);
        if (timer.Interval != interval) timer.Interval = interval;
    }
    private void ExecuteCommand(string command)
    {
        if (disposed) return;
        try
        {
            completingKind = engine.Kind;
            switch (command)
            {
                case "Bắt đầu": case "Bắt đầu / Tiếp tục": engine.Start(); break;
                case "Tạm dừng": engine.Pause(); break;
                case "Tiếp tục": engine.Resume(); break;
                case "Bỏ qua": engine.Skip(); break;
                case "Đặt lại": engine.Reset(); break;
            }
            RefreshPresentation(); SaveSession();
        }
        catch (Exception ex) { Log(ex); System.Windows.MessageBox.Show(ex.Message, "Nekodoro"); }
    }
    internal void ShowFocus()
    {
        if (disposed) return;
        tray.EnsureVisible();
        if (focusWindow is null)
        {
            focusWindow = new FocusWindow(ExecuteCommand, ShowSettings);
            var window = focusWindow;
            window.Closing += (_, e) => GuardCloseToTray(window, e);
            window.Closed += (_, _) => focusWindow = null;
        }
        focusWindow.UpdateStatus(engine, settings.Pomodoro.LongBreakEvery);
        focusWindow.Show(); focusWindow.WindowState = System.Windows.WindowState.Normal; focusWindow.Activate();
    }
    private void GuardCloseToTray(System.Windows.Window window, System.ComponentModel.CancelEventArgs e)
    {
        if (disposed || tray.EnsureVisible()) return;
        e.Cancel = true;
        System.Windows.MessageBox.Show(window, "Chưa thể tạo biểu tượng ở khay hệ thống. Cửa sổ được giữ mở để bạn tiếp tục điều khiển Nekodoro.", "Nekodoro");
    }
    internal void ShowSettings()
    {
        if (disposed) return;
        tray.EnsureVisible();
        if (settingsWindow is not null)
        {
            settingsWindow.Show(); settingsWindow.WindowState = System.Windows.WindowState.Normal;
            settingsWindow.Activate(); return;
        }
        settingsWindow = new SettingsWindow(settings, draft => { overlay.Settings = draft; RefreshPresentation(); }, draft =>
        {
            draft.Validate(); if (draft.LaunchAtStartup != settings.LaunchAtStartup) ApplyStartup(draft.LaunchAtStartup); store.SaveSettings(draft);
            // Settings remain open after Save, so the draft must not alias committed settings.
            settings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(draft))!;
            engine.UpdateSettings(settings.Pomodoro); overlay.Settings = settings; RefreshPresentation();
        }, ExecuteCommand);
        var window = settingsWindow;
        settingsWindow.Closing += (_, e) => GuardCloseToTray(window, e);
        settingsWindow.EditPosition += enabled => { overlay.Editing = enabled; RefreshPresentation(); };
        settingsWindow.Closed += (_, _) => { settingsWindow = null; overlay.Editing = false; overlay.Settings = settings; if (!disposed) RefreshPresentation(); };
        settingsWindow.Show();
    }
    private void ApplyStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled)
        {
            string exe = Environment.ProcessPath ?? throw new InvalidOperationException("Không tìm thấy ứng dụng.");
            string command = $"\"{exe}\"";
            if (!string.Equals(key.GetValue("PomodoroTaskbar") as string, command, StringComparison.OrdinalIgnoreCase))
                key.SetValue("PomodoroTaskbar", command);
        }
        else key.DeleteValue("PomodoroTaskbar", false);
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) System.Windows.Application.Current.Dispatcher.Invoke(() => { completingKind = engine.Kind; engine.Pause(); RefreshPresentation(); SaveSession(); });
    }
    private void DisplayChanged(object? sender, EventArgs e) => System.Windows.Application.Current.Dispatcher.BeginInvoke(() => { overlay.InvalidateLayout(); RefreshPresentation(); });
    private void SaveSession()
    {
        var snapshot = engine.Snapshot(); checkpointClock.Restart();
        pendingSave = pendingSave.ContinueWith(_ =>
        {
            try { store.SaveSession(snapshot); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Log(ex); }
        }, TaskScheduler.Default);
    }
    private void Log(Exception ex)
    {
        try { Directory.CreateDirectory(directory); var path = Path.Combine(directory, "diagnostic.log"); if (File.Exists(path) && new FileInfo(path).Length > 256_000) File.Delete(path); File.AppendAllText(path, $"{DateTime.UtcNow:O} {ex}\n"); } catch { }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        timer.Stop(); completingKind = engine.Kind; engine.Pause(); SaveSession(); pendingSave.GetAwaiter().GetResult();
        SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        focusWindow?.Close(); settingsWindow?.Close(); tray.Dispose(); overlay.Close();
    }
    private static string KindLabel(SessionKind kind) => kind switch
    {
        SessionKind.Focus => "Tập trung", SessionKind.ShortBreak => "Nghỉ ngắn", _ => "Nghỉ dài"
    };
}
