using System.Threading;
using System.Windows;

namespace PomodoroTaskbar.Windows;

public partial class App : System.Windows.Application
{
    private Mutex? mutex;
    private ApplicationController? controller;
    private EventWaitHandle? showRequested;
    private RegisteredWaitHandle? showWait;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        mutex = new Mutex(true, "Local\\PomodoroTaskbar.Mvp", out var created);
        if (!created)
        {
            // A second launch restores the running app instead of silently disappearing.
            try { using var signal = EventWaitHandle.OpenExisting("Local\\PomodoroTaskbar.ShowSettings"); signal.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            Shutdown(); return;
        }
        var dataIndex = Array.IndexOf(e.Args, "--data-dir");
        controller = new ApplicationController(dataIndex >= 0 && dataIndex + 1 < e.Args.Length ? e.Args[dataIndex + 1] : null);
        showRequested = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\PomodoroTaskbar.ShowSettings");
        showWait = ThreadPool.RegisterWaitForSingleObject(showRequested, (_, _) => Dispatcher.BeginInvoke(() => controller.ShowFocus()), null, Timeout.Infinite, false);
        controller.Start();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        showWait?.Unregister(null);
        showRequested?.Dispose();
        controller?.Dispose();
        mutex?.Dispose();
        base.OnExit(e);
    }
}
