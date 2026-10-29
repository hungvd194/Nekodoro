using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using PomodoroTaskbar.Core;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace PomodoroTaskbar.Windows;

/// <summary>Owns a native notification icon on the WPF dispatcher; no WinForms message loop is required.</summary>
internal sealed class TrayIconService : IDisposable
{
    private const uint CallbackMessage = 0x8001;
    private const uint MessageFlag = 1, IconFlag = 2, TipFlag = 4, InfoFlag = 0x10, GuidFlag = 0x20, ShowTipFlag = 0x80;
    private readonly Guid iconGuid;
    private readonly HwndSource source;
    private readonly uint taskbarCreated;
    private readonly Icon icon;
    private readonly ContextMenu menu = new();
    private readonly Dictionary<string, MenuItem> commands = [];
    private bool added;
    private bool disposed;
    private string tooltip = "Nekodoro";
    private (SessionKind Kind, TimerState State, int Seconds)? lastStatus;
    internal int LastError { get; private set; }
    internal bool IsRegistered => added && !disposed;
    internal event Action? OpenSettingsRequested;
    internal event Action? OpenFocusRequested;
    internal event Action? ExitRequested;
    internal event Action<string>? CommandRequested;

    internal TrayIconService(Guid? identity = null)
    {
        iconGuid = identity ?? new Guid("53f4f414-4a52-4659-bf0d-06034530c22d");
        icon = AppIcon.TrayImage();
        taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        // A hidden top-level HWND receives Explorer's registered broadcast after a restart.
        // HWND_MESSAGE would not receive that broadcast.
        source = new HwndSource(new HwndSourceParameters("PomodoroTaskbar.TrayHost")
        {
            Width = 0, Height = 0, PositionX = 0, PositionY = 0,
            WindowStyle = unchecked((int)0x80000000), ExtendedWindowStyle = 0x80,
            ParentWindow = IntPtr.Zero
        });
        source.AddHook(WindowMessage);
        foreach (var name in new[] { "Bắt đầu", "Tạm dừng", "Tiếp tục", "Bỏ qua", "Đặt lại" })
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) => CommandRequested?.Invoke(name);
            commands.Add(name, item); menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var focus = new MenuItem { Header = "Mở Nekodoro" };
        focus.Click += (_, _) => OpenFocusRequested?.Invoke(); menu.Items.Add(focus);
        var settings = new MenuItem { Header = "Cài đặt" };
        settings.Click += (_, _) => OpenSettingsRequested?.Invoke(); menu.Items.Add(settings);
        var exit = new MenuItem { Header = "Thoát" };
        exit.Click += (_, _) => ExitRequested?.Invoke(); menu.Items.Add(exit);
        menu.Closed += (_, _) => PostMessage(source.Handle, 0, IntPtr.Zero, IntPtr.Zero);
        EnsureVisible();
    }

    /// <summary>Checks registration with Explorer and retries adding if it was lost.</summary>
    internal bool EnsureVisible()
    {
        if (disposed) return false;
        var data = Data(MessageFlag | IconFlag | TipFlag | GuidFlag | ShowTipFlag);
        if (added && Shell_NotifyIcon(1, ref data)) return true;
        added = Shell_NotifyIcon(0, ref data);
        // An icon left registered after a failed modification may already exist.
        if (!added) added = Shell_NotifyIcon(1, ref data);
        if (!added)
        {
            LastError = Marshal.GetLastWin32Error();
            Trace.TraceWarning($"Shell_NotifyIcon registration failed (Win32 {LastError}).");
            return false;
        }
        data.VersionOrTimeout = 4;
        if (!Shell_NotifyIcon(4, ref data))
            Trace.TraceWarning("Shell_NotifyIcon could not enable version 4 callbacks; legacy callbacks remain supported.");
        LastError = 0;
        return true;
    }

    internal void Update(PomodoroEngine engine)
    {
        if (disposed) return;
        int seconds = (int)Math.Ceiling(engine.Remaining.TotalSeconds);
        var current = (engine.Kind, engine.State, seconds);
        if (lastStatus == current && added) return;
        lastStatus = current;
        commands["Bắt đầu"].IsEnabled = engine.State is TimerState.Idle or TimerState.AwaitingNext;
        commands["Tạm dừng"].IsEnabled = engine.State == TimerState.Running;
        commands["Tiếp tục"].IsEnabled = engine.State == TimerState.Paused;
        var kind = engine.Kind == SessionKind.Focus ? "Tập trung" : engine.Kind == SessionKind.ShortBreak ? "Nghỉ ngắn" : "Nghỉ dài";
        string text = $"Nekodoro · {kind} · {seconds / 60:00}:{seconds % 60:00}";
        if (tooltip == text && added) return;
        tooltip = text;
        if (!added) { EnsureVisible(); return; }
        var data = Data(TipFlag | GuidFlag | ShowTipFlag);
        if (!Shell_NotifyIcon(1, ref data)) { added = false; EnsureVisible(); }
    }

    internal void ShowNotification(string title, string message)
    {
        if (!EnsureVisible()) return;
        var data = Data(GuidFlag | InfoFlag);
        data.InfoTitle = title.Length > 63 ? title[..63] : title;
        data.Info = message.Length > 255 ? message[..255] : message;
        data.InfoFlags = 1;
        Shell_NotifyIcon(1, ref data);
    }

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (disposed) return IntPtr.Zero;
        if ((uint)message == taskbarCreated && taskbarCreated != 0)
        {
            added = false; EnsureVisible(); handled = true;
        }
        else if ((uint)message == CallbackMessage)
        {
            int notification = (int)(lParam.ToInt64() & 0xffff);
            switch (notification)
            {
                case 0x0202: // WM_LBUTTONUP (legacy)
                case 0x0203: // WM_LBUTTONDBLCLK
                case 0x0400: // NIN_SELECT
                case 0x0401: // NIN_KEYSELECT
                    OpenFocusRequested?.Invoke();
                    break;
                case 0x0205: // WM_RBUTTONUP (legacy)
                case 0x007b: // WM_CONTEXTMENU (version 4)
                    SetForegroundWindow(source.Handle);
                    menu.Placement = PlacementMode.MousePoint;
                    menu.IsOpen = true;
                    break;
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private NotifyIconData Data(uint flags) => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = source.Handle, Id = 1,
        Flags = flags, CallbackMessage = CallbackMessage, Icon = icon.Handle,
        Tip = tooltip, Info = "", InfoTitle = "", Guid = iconGuid
    };

    public void Dispose()
    {
        if (disposed) return;
        menu.IsOpen = false;
        var data = Data(GuidFlag); Shell_NotifyIcon(2, ref data);
        disposed = true; added = false;
        source.RemoveHook(WindowMessage); source.Dispose(); icon.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id, Flags, CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint VersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}

