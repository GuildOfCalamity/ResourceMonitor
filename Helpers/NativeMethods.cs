using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.Helpers;

#region [Supporting Structs]
[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public INPUTTYPE Type;
    public InputUnion Data;

    public static int Size
    {
        get { return Marshal.SizeOf(typeof(INPUT)); }
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)]
    internal MOUSEINPUT mi;
    [FieldOffset(0)]
    internal KEYBDINPUT ki;
    [FieldOffset(0)]
    internal HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    internal int dx;
    internal int dy;
    internal int mouseData;
    internal uint dwFlags;
    internal uint time;
    internal UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    internal short wVk;
    internal short wScan;
    internal uint dwFlags;
    internal int time;
    internal UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HARDWAREINPUT
{
    internal int uMsg;
    internal short wParamL;
    internal short wParamH;
}

internal enum INPUTTYPE : uint
{
    INPUTMOUSE = 0,
    INPUTKEYBOARD = 1,
    INPUTHARDWARE = 2,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class OpenFileName
{
    public int StructSize;
    public IntPtr Hwnd = IntPtr.Zero;
    public IntPtr Hinst = IntPtr.Zero;
    public string Filter;
    public string CustFilter;
    public int CustFilterMax;
    public int FilterIndex;
    public string File;
    public int MaxFile;
    public string FileTitle;
    public int MaxFileTitle;
    public string InitialDir;
    public string Title;
    public int Flags;
    public short FileOffset;
    public short FileExtMax;
    public string DefExt;
    public int CustData;
    public IntPtr Hook = IntPtr.Zero;
    public string Template;
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
internal struct WINDOWPLACEMENT
{
    public int Length { get; set; }
    public int Flags { get; set; }
    public int ShowCmd { get; set; }
    public POINT MinPosition { get; set; }
    public POINT MaxPosition { get; set; }
    public RECT NormalPosition { get; set; }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X { get; set; }
    public int Y { get; set; }
    public POINT(int x, int y)
    {
        X = x;
        Y = y;
    }
}
#endregion

[SuppressUnmanagedCodeSecurity]
internal static class NativeMethods
{
    public static int GWL_STYLE = -16;          // WPF's Message code for Title Bar's Style
    public static int GWL_EX_STYLE = -20;
    public static int WS_SYSMENU = 0x80000;     // WPF's Message code for System Menu
    public static uint WS_SIZEBOX = 0x00040000;
    public static int WS_BORDER = 0x00800000;   // window with border
    public static int WS_DLGFRAME = 0x00400000; // window with double border but no title
    public static int WS_POPUP = 1 << 31;       // 0x80000000
    public static int WS_CAPTION = WS_BORDER | WS_DLGFRAME; // window with a title bar
    public static int WS_EX_TOOLWINDOW = 0x00000080;
    public static int WM_NCCALCSIZE = 0x0083;
    public static int WM_NCACTIVATE = 0x0086;
    public static int WM_NCPAINT = 0x0085;
    public static int WM_NCLBUTTONDOWN = 0x00A1;
    public static int WM_SYSCOMMAND = 0x0112;
    public static int SPI_GETDESKWALLPAPER = 0x0073;
    public static int SC_MOVE = 0xF010;
    public static int SW_HIDE = 0;
    public static int SW_SHOWNORMAL = 1;
    public static int SW_SHOWMAXIMIZED = 3;
    public static int SW_RESTORE = 9;

    // For setting window icon.
    public static int ICON_SMALL = 0;
    public static int ICON_BIG = 1;
    public static int ICON_SMALL2 = 2;
    public static uint WM_GETICON = 0x007F;
    public static uint WM_SETICON = 0x0080;

    /// <summary>
    /// Delegate declaration that matches WndProc signatures.
    /// </summary>
    public delegate IntPtr MessageHandler(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled);

    [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Shell32CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);

    [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
    internal static extern IntPtr Kernel32LocalFree(IntPtr hMem);

    [DllImport("user32.dll")]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

    [DllImport("user32")]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32")]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    internal static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

    [DllImportAttribute("User32.dll")]
    internal static extern int FindWindow(String ClassName, String WindowName);

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("User32.dll")]
    public static extern bool ShowWindow(IntPtr handle, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("User32.dll")]
    public static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    internal static extern IntPtr GetShellWindow();

    [DllImport("Comdlg32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

    [DllImport("shell32.dll")]
    internal static extern IntPtr SHBrowseForFolderW(ref ShellGetFolder.BrowseInformation browseInfo);

    [DllImport("shell32.dll")]
    internal static extern int SHGetPathFromIDListW(IntPtr pidl, IntPtr pszPath);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowRect(IntPtr hwnd, out RECT rc);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    internal static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.DLL", CharSet = CharSet.Unicode)]
    internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    internal static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    internal static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    public static string[] CommandLineToArgvW(string cmdLine)
    {
        IntPtr argv = IntPtr.Zero;
        try
        {
            argv = Shell32CommandLineToArgvW(cmdLine, out int numArgs);
            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            var result = new string[numArgs];

            for (int i = 0; i < numArgs; i++)
            {
                IntPtr currArg = Marshal.ReadIntPtr(argv, i * Marshal.SizeOf(typeof(IntPtr)));
                result[i] = Marshal.PtrToStringUni(currArg);
            }

            return result;
        }
        finally
        {
            _ = Kernel32LocalFree(argv);

            // Otherwise LocalFree failed.
            // Assert.AreEqual(IntPtr.Zero, p);
        }
    }

    #region [Topmost Helper]
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
    const UInt32 SWP_NOSIZE = 0x0001;
    const UInt32 SWP_NOMOVE = 0x0002;
    const UInt32 SWP_SHOWWINDOW = 0x0040;
    static readonly IntPtr HWND_TOP = new IntPtr(0); // Places the window at the top of the Z order. 
    static readonly IntPtr HWND_BOTTOM = new IntPtr(1); // Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows. 
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated. 
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window. 
    const int RetryTopMostDelay = 200;
    const int RetryTopMostMax = 4;
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_TOPMOST = 0x00000008;
    /// <summary>
    /// The code below will retry several times before giving up.
    /// This typically works using only one retry.
    /// </summary>
    /// <param name="hwnd">The main window's <see cref="IntPtr"/></param>
    /// <returns>don't worry about it</returns>
    internal static async Task RetryTopMost(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            for (int i = 0; i < RetryTopMostMax; i++)
            {
                await Task.Delay(RetryTopMostDelay);
                int winStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                if ((winStyle & WS_EX_TOPMOST) != 0)
                    break; // already topmost
                else
                    Debug.WriteLine("NOTICE: Window is not topmost.");

                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RetryTopMost: {ex.Message}");
        }
    }

    /// <summary>
    /// The code below will retry several times before giving up.
    /// This typically works using only one retry.
    /// </summary>
    /// <param name="hwnd">The main window's <see cref="IntPtr"/></param>
    /// <returns>don't worry about it</returns>
    internal static async Task UndoTopMost(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            for (int i = 0; i < RetryTopMostMax / 2; i++)
            {
                await Task.Delay(RetryTopMostDelay);
                int winStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                if ((winStyle & WS_EX_TOPMOST) != 0) // are we currently topmost?
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                else
                    Debug.WriteLine("NOTICE: Window is not topmost.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UndoTopMost: {ex.Message}");
        }
    }
    #endregion [Topmost Helper]
}

public class ShellGetFolder
{
    public delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp);

    [StructLayout(LayoutKind.Sequential)]
    public struct BrowseInformation
    {
        public IntPtr HwndOwner;
        public IntPtr PidlRoot;
        public string PszDisplayName;
        public string LpszTitle;
        public uint UlFlags;
        public BrowseCallbackProc Lpfn;
        public IntPtr LParam;
        public int IImage;
    }

    public static string GetFolderDialog(IntPtr hwndOwner)
    {
        // windows MAX_PATH with long path enable can be approximated 32k char long
        // allocating more than double (unicode) to hold the path
        StringBuilder sb = new StringBuilder(65000);
        IntPtr bufferAddress = Marshal.AllocHGlobal(65000);
        IntPtr pidl = IntPtr.Zero;
        BrowseInformation browseInfo;
        browseInfo.HwndOwner = hwndOwner;
        browseInfo.PidlRoot = IntPtr.Zero;
        browseInfo.PszDisplayName = null;
        browseInfo.LpszTitle = null;
        browseInfo.UlFlags = 0;
        browseInfo.Lpfn = null;
        browseInfo.LParam = IntPtr.Zero;
        browseInfo.IImage = 0;

        try
        {
            pidl = NativeMethods.SHBrowseForFolderW(ref browseInfo);
            if (NativeMethods.SHGetPathFromIDListW(pidl, bufferAddress) == 0)
            {
                return null;
            }

            sb.Append(Marshal.PtrToStringUni(bufferAddress));
            Marshal.FreeHGlobal(bufferAddress);
        }
        finally
        {
            // Need to free pidl
            Marshal.FreeCoTaskMem(pidl);
        }

        return sb.ToString();
    }
}

public class ScreenInformation
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [DllImport("user32")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

    private delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref ScreenRect pRect, int dwData);

    public static int GetMonitorCount()
    {
        int monCount = 0;
        MonitorEnumProc callback = (IntPtr hDesktop, IntPtr hdc, ref ScreenRect prect, int d) => ++monCount > 0;

        if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, 0))
            Console.WriteLine("You have {0} monitors", monCount);
        else
            Console.WriteLine("An error occured while enumerating monitors");

        if (monCount == 0)
            monCount = 1;

        return monCount;
    }
}

#region [Enums]
// https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerhotkey
internal enum HOTKEY_MODIFIERS : uint
{
    ALT = 0x0001,
    CONTROL = 0x0002,
    SHIFT = 0x0004,
    WIN = 0x0008,
    NOREPEAT = 0x4000,
    CHECK_FLAGS = 0x000F, // modifiers to compare between keys.
}

// https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-showwindow
internal enum SW : int
{
    HIDE = 0x0000,
    SHOWNORMAL = 0x0001,
    SHOWMINIMIZED = 0x0002,
    SHOWMAXIMIZED = 0x0003,
    SHOWNOACTIVATE = 0x0004,
    SHOW = 0x0005,
    MINIMIZE = 0x0006,
    SHOWMINNOACTIVE = 0x0007,
    SHOWNA = 0x0008,
    RESTORE = 0x0009,
    SHOWDEFAULT = 0x000A,
    FORCEMINIMIZE = 0x000B,
}

internal enum WM
{
    NULL = 0x0000,
    CREATE = 0x0001,
    DESTROY = 0x0002,
    MOVE = 0x0003,
    SIZE = 0x0005,
    ACTIVATE = 0x0006,
    SETFOCUS = 0x0007,
    KILLFOCUS = 0x0008,
    ENABLE = 0x000A,
    SETREDRAW = 0x000B,
    SETTEXT = 0x000C,
    GETTEXT = 0x000D,
    GETTEXTLENGTH = 0x000E,
    PAINT = 0x000F,
    CLOSE = 0x0010,
    QUERYENDSESSION = 0x0011,
    QUIT = 0x0012,
    QUERYOPEN = 0x0013,
    ERASEBKGND = 0x0014,
    SYSCOLORCHANGE = 0x0015,
    SHOWWINDOW = 0x0018,
    SETTINGCHANGE = 0x001A,
    ACTIVATEAPP = 0x001C,
    SETCURSOR = 0x0020,
    MOUSEACTIVATE = 0x0021,
    CHILDACTIVATE = 0x0022,
    QUEUESYNC = 0x0023,
    GETMINMAXINFO = 0x0024,
    WINDOWPOSCHANGING = 0x0046,
    WINDOWPOSCHANGED = 0x0047,
    CONTEXTMENU = 0x007B,
    STYLECHANGING = 0x007C,
    STYLECHANGED = 0x007D,
    DISPLAYCHANGE = 0x007E,
    GETICON = 0x007F,
    SETICON = 0x0080,
    NCCREATE = 0x0081,
    NCDESTROY = 0x0082,
    NCCALCSIZE = 0x0083,
    NCHITTEST = 0x0084,
    NCPAINT = 0x0085,
    NCACTIVATE = 0x0086,
    GETDLGCODE = 0x0087,
    SYNCPAINT = 0x0088,
    NCMOUSEMOVE = 0x00A0,
    NCLBUTTONDOWN = 0x00A1,
    NCLBUTTONUP = 0x00A2,
    NCLBUTTONDBLCLK = 0x00A3,
    NCRBUTTONDOWN = 0x00A4,
    NCRBUTTONUP = 0x00A5,
    NCRBUTTONDBLCLK = 0x00A6,
    NCMBUTTONDOWN = 0x00A7,
    NCMBUTTONUP = 0x00A8,
    NCMBUTTONDBLCLK = 0x00A9,
    SYSKEYDOWN = 0x0104,
    SYSKEYUP = 0x0105,
    SYSCHAR = 0x0106,
    SYSDEADCHAR = 0x0107,
    COMMAND = 0x0111,
    SYSCOMMAND = 0x0112,
    MOUSEMOVE = 0x0200,
    LBUTTONDOWN = 0x0201,
    LBUTTONUP = 0x0202,
    LBUTTONDBLCLK = 0x0203,
    RBUTTONDOWN = 0x0204,
    RBUTTONUP = 0x0205,
    RBUTTONDBLCLK = 0x0206,
    MBUTTONDOWN = 0x0207,
    MBUTTONUP = 0x0208,
    MBUTTONDBLCLK = 0x0209,
    MOUSEWHEEL = 0x020A,
    XBUTTONDOWN = 0x020B,
    XBUTTONUP = 0x020C,
    XBUTTONDBLCLK = 0x020D,
    MOUSEHWHEEL = 0x020E,
    CAPTURECHANGED = 0x0215,
    ENTERSIZEMOVE = 0x0231,
    EXITSIZEMOVE = 0x0232,
    IME_SETCONTEXT = 0x0281,
    IME_NOTIFY = 0x0282,
    IME_CONTROL = 0x0283,
    IME_COMPOSITIONFULL = 0x0284,
    IME_SELECT = 0x0285,
    IME_CHAR = 0x0286,
    IME_REQUEST = 0x0288,
    IME_KEYDOWN = 0x0290,
    IME_KEYUP = 0x0291,
    NCMOUSELEAVE = 0x02A2,
    HOTKEY = 0x0312,
    DWMCOMPOSITIONCHANGED = 0x031E,
    DWMNCRENDERINGCHANGED = 0x031F,
    DWMCOLORIZATIONCOLORCHANGED = 0x0320,
    DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
    DWMSENDICONICTHUMBNAIL = 0x0323,
    DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
    USER = 0x0400,
    // This is the hard-coded message value used by WinForms for Shell_NotifyIcon.
    TRAYMOUSEMESSAGE = 0x800, // WM_USER + 1024
    APP = 0x8000,
}
#endregion
