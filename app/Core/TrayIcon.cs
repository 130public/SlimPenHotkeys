using System.Runtime.InteropServices;

namespace SlimPenHotkeys.Core;

/// <summary>
/// System-tray icon for a WinUI 3 app, implemented directly on top of
/// Shell_NotifyIcon with a hidden message-only window (WinUI 3 has no built-in
/// tray support). Provides a Show / Toggle All / Exit context menu.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    public event Action? ShowRequested;
    public event Action? ToggleRequested;
    public event Action? ExitRequested;

    private const uint WM_APP_TRAY = 0x8000 + 1; // WM_APP + 1
    private const uint NIM_ADD = 0x0;
    private const uint NIM_MODIFY = 0x1;
    private const uint NIM_DELETE = 0x2;
    private const uint NIF_MESSAGE = 0x1;
    private const uint NIF_ICON = 0x2;
    private const uint NIF_TIP = 0x4;

    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_COMMAND = 0x0111;

    private const uint MF_STRING = 0x0;
    private const uint MF_SEPARATOR = 0x800;
    private const uint TPM_RIGHTBUTTON = 0x2;
    private const uint TPM_RETURNCMD = 0x100;

    private const int IDM_SHOW = 1;
    private const int IDM_TOGGLE = 2;
    private const int IDM_EXIT = 3;

    private readonly WndProc _wndProc;   // kept alive to avoid GC
    private readonly string _className = "SlimPenHotkeysTray_" + Guid.NewGuid().ToString("N");
    private IntPtr _hwnd;
    private IntPtr _hIcon;
    private bool _iconOwned;   // true when _hIcon was created by us and must be destroyed
    private uint _iconId = 1;
    private bool _created;

    public TrayIcon()
    {
        _wndProc = WindowProc;
    }

    /// <summary>Create the hidden window and add the tray icon. Call on the UI thread.</summary>
    public void Create(string tooltip)
    {
        var wc = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = _className,
        };
        RegisterClass(ref wc);

        _hwnd = CreateWindowEx(0, _className, "SlimPenHotkeysTray", 0, 0, 0, 0, 0,
            HWND_MESSAGE, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

        _hIcon = LoadThemeIcon();

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = _iconId,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_APP_TRAY,
            hIcon = _hIcon,
            szTip = Trunc(tooltip, 127),
        };
        Shell_NotifyIcon(NIM_ADD, ref nid);
        _created = true;
    }

    public void SetTooltip(string tooltip)
    {
        if (!_created) return;
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = _iconId,
            uFlags = NIF_TIP,
            szTip = Trunc(tooltip, 127),
        };
        Shell_NotifyIcon(NIM_MODIFY, ref nid);
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_APP_TRAY)
        {
            uint mouse = (uint)(lParam.ToInt64() & 0xFFFF);
            if (mouse == WM_LBUTTONDBLCLK)
            {
                ShowRequested?.Invoke();
            }
            else if (mouse == WM_RBUTTONUP || mouse == WM_LBUTTONUP)
            {
                ShowContextMenu();
            }
            return IntPtr.Zero;
        }
        if (msg == WM_COMMAND)
        {
            int cmd = (int)(wParam.ToInt64() & 0xFFFF);
            switch (cmd)
            {
                case IDM_SHOW: ShowRequested?.Invoke(); break;
                case IDM_TOGGLE: ToggleRequested?.Invoke(); break;
                case IDM_EXIT: ExitRequested?.Invoke(); break;
            }
            return IntPtr.Zero;
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Reloads the tray icon for the current system theme (call when the Windows
    /// theme changes) so it stays visible against the light or dark taskbar.
    /// </summary>
    public void UpdateIcon()
    {
        if (!_created) return;
        IntPtr old = _hIcon;
        bool oldOwned = _iconOwned;
        _hIcon = LoadThemeIcon();
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = _iconId,
            uFlags = NIF_ICON,
            hIcon = _hIcon,
        };
        Shell_NotifyIcon(NIM_MODIFY, ref nid);
        if (oldOwned && old != IntPtr.Zero) DestroyIcon(old);
    }

    private void ShowContextMenu()
    {
        IntPtr menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, IDM_SHOW, "Show SlimPen Hotkeys");
        AppendMenu(menu, MF_STRING, IDM_TOGGLE, "Toggle All");
        AppendMenu(menu, MF_SEPARATOR, 0, null);
        AppendMenu(menu, MF_STRING, IDM_EXIT, "Exit");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hwnd); // required so the menu dismisses on focus loss
        int cmd = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.x, pt.y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(menu);

        switch (cmd)
        {
            case IDM_SHOW: ShowRequested?.Invoke(); break;
            case IDM_TOGGLE: ToggleRequested?.Invoke(); break;
            case IDM_EXIT: ExitRequested?.Invoke(); break;
        }
    }

    private IntPtr LoadThemeIcon()
    {
        try
        {
            string path = ThemeIcons.SystemIconPath();
            if (File.Exists(path))
            {
                IntPtr h = LoadImage(IntPtr.Zero, path, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                if (h != IntPtr.Zero)
                {
                    _iconOwned = true;
                    return h;
                }
            }
        }
        catch { /* fall through */ }
        _iconOwned = false;
        return LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION (shared, do not destroy)
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

    public void Dispose()
    {
        if (_created)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = _iconId,
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            _created = false;
        }
        if (_iconOwned && _hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
            _iconOwned = false;
        }
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
        UnregisterClass(_className, GetModuleHandle(null));
    }

    // ---- Interop --------------------------------------------------------
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static readonly IntPtr HWND_MESSAGE = new(-3);
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x10;
    private const uint LR_DEFAULTSIZE = 0x40;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
