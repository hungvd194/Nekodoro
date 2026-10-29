using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Text;
using Forms = System.Windows.Forms;

namespace PomodoroTaskbar.Windows;

internal static class Native
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int index, int value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr h, uint command);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RectNative rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
    internal static bool RestoreOverlayOrder(IntPtr handle)
    {
        if (!GetWindowRect(handle, out var bar)) return false;
        // Taskbar activation can reorder topmost windows without changing our geometry.
        // Only repair an actual overlap; keep normal timer ticks free of SetWindowPos.
        var above = GetWindow(handle, 3); // GW_HWNDPREV
        for (int i = 0; above != IntPtr.Zero && i < 128; i++, above = GetWindow(above, 3))
        {
            if (!IsWindowVisible(above) || !GetWindowRect(above, out var other)) continue;
            if (other.Left < bar.Right && other.Right > bar.Left && other.Top < bar.Bottom && other.Bottom > bar.Top)
                return SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x13); // NOSIZE | NOMOVE | NOACTIVATE
        }
        return false;
    }
    internal static void ClickThrough(Window window, bool enabled)
    {
        var h = new WindowInteropHelper(window).Handle;
        ClickThrough(h, enabled);
    }
    internal static void ClickThrough(IntPtr h, bool enabled)
    {
        var previous = GetWindowLong(h, -20);
        var style = previous | 0x08000000 | 0x80;
        style = enabled ? style | 0x20 : style & ~0x20;
        if (style != previous) SetWindowLong(h, -20, style);
    }
    internal static bool Fullscreen(Forms.Screen screen, IntPtr own)
    {
        var h = GetForegroundWindow();
        if (h == IntPtr.Zero || h == own || !GetWindowRect(h, out var r)) return false;
        var b = screen.Bounds;
        if (r.Left > b.Left || r.Top > b.Top || r.Right < b.Right || r.Bottom < b.Bottom || IsZoomed(h)) return false;
        var name = new StringBuilder(128); GetClassName(h, name, name.Capacity);
        return name.ToString() is not ("Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd");
    }
}
