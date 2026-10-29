using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PomodoroTaskbar.Windows;
using Button = System.Windows.Controls.Button;

internal static class PerformanceProbe
{
    internal static int Run()
    {
        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var samples = new List<object>();
        var data = System.IO.Path.Combine(Environment.CurrentDirectory, ".test-data", "Performance-" + Guid.NewGuid());
        try
        {
            using var controller = new ApplicationController(data, Guid.NewGuid());
            controller.Start(); Pump(2);
            var overlay = app.Windows.OfType<OverlayWindow>().Single();
            Sample("focus-open-idle");
            Focus().Close(); Pump(1); Sample("tray-idle");
            controller.ShowFocus(); Click("Bắt đầu"); Focus().Close(); Pump(1); Sample("tray-running");
            controller.ShowFocus(); Click("Tạm dừng"); Focus().Close(); Pump(1); Sample("tray-paused");
            controller.ShowFocus(); Focus().Close(); Pump(.2);
            Console.WriteLine(JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
            return 0;

            FocusWindow Focus() => app.Windows.OfType<FocusWindow>().Single(w => w.IsVisible);
            void Click(string text)
            {
                Focus().UpdateLayout();
                Descendants<Button>(Focus()).Single(b => b.Content as string == text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            void Sample(string name)
            {
                var process = Process.GetCurrentProcess(); process.Refresh();
                var cpu = process.TotalProcessorTime;
                long allocated = GC.GetTotalAllocatedBytes();
                int geometry = overlay.GeometryUpdateCount, styles = overlay.ClickThroughUpdateCount, renders = overlay.RenderCount;
                var watch = Stopwatch.StartNew(); Pump(6); watch.Stop(); process.Refresh();
                samples.Add(new {
                    state = name, seconds = Math.Round(watch.Elapsed.TotalSeconds, 2),
                    cpuOneCorePercent = Math.Round((process.TotalProcessorTime - cpu).TotalMilliseconds / watch.Elapsed.TotalMilliseconds * 100, 2),
                    allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
                    workingSetMiB = Math.Round(process.WorkingSet64 / 1048576.0, 1),
                    privateMiB = Math.Round(process.PrivateMemorySize64 / 1048576.0, 1),
                    handles = process.HandleCount,
                    renderCalls = overlay.RenderCount - renders,
                    windowPositionCalls = overlay.GeometryUpdateCount - geometry,
                    clickThroughCalls = overlay.ClickThroughUpdateCount - styles
                });
            }
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { app.Shutdown(); }
    }

    private static void Pump(double seconds)
    {
        var frame = new DispatcherFrame();
        var stop = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        stop.Tick += (_, _) => { stop.Stop(); frame.Continue = false; };
        stop.Start(); Dispatcher.PushFrame(frame);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
