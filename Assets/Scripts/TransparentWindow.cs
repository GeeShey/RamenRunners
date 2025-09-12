using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
    [Range(0, 255)]
    public int WindowOpacity = 255;

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    private IntPtr hwnd;
    private int currentMonitorIndex = 0;
    private bool isBorderless = false; // Track current window state - start in bordered mode

    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const int WS_EX_LAYERED = 0x80000;
    private const int LWA_COLORKEY = 0x00000001;
    private const int LWA_ALPHA = 0x00000002;

    // Window styles
    private const int WS_OVERLAPPEDWINDOW = 0x00CF0000; // Standard window with borders
    private const int WS_POPUP = unchecked((int)0x80000000); // Borderless window

    // ShowWindow constants
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static TransparentWindow instance;

    void Start()
    {
        instance = this;
        if (!Application.isEditor)
        {
            hwnd = GetActiveWindow();

            // Start in bordered mode - remove layered window style to make it fully opaque
            SetWindowLong(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) & ~WS_EX_LAYERED);
            Screen.fullScreenMode = FullScreenMode.Windowed;

            // Force complete window update with frame change
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            // Restore window position and size if calibration was completed
            RestoreWindowPosition();
        }
    }

    void Update()
    {
        // Check for F3 key press
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ToggleWindowBorder();
        }

        // Check for docking keys (only work in borderless mode)
        if (isBorderless && !Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                DockWindowRight();
            }
        }
    }

    public void ToggleWindowBorder()
    {
        if (!Application.isEditor)
        {
            isBorderless = !isBorderless;

            if (isBorderless)
            {
                // Set to borderless mode with transparency
                SetWindowLong(hwnd, GWL_STYLE, WS_POPUP);
                // Enable layered window for transparency
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED);
                // Apply transparency
                SetLayeredWindowAttributes(hwnd, 0, (byte)this.WindowOpacity, LWA_COLORKEY | LWA_ALPHA);
            }
            else
            {
                // Set to windowed mode with borders - remove transparency
                SetWindowLong(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);
                // Remove layered window style to make it fully opaque
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) & ~WS_EX_LAYERED);
                // Force Unity to recognize the change
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }

            // Force complete window update with frame change
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            // Only reapply transparency if in borderless mode
            if (isBorderless)
            {
                SetLayeredWindowAttributes(hwnd, 0, (byte)this.WindowOpacity, LWA_COLORKEY | LWA_ALPHA);
            }

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }
        else
        {
            Debug.Log("ToggleWindowBorder runs only in the Windows build.");
        }
    }

    private void DockWindowRight()
    {
        if (!Application.isEditor)
        {
            // Get the monitor info for the current monitor
            IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                // Calculate right half of the screen (work area excludes taskbar)
                int screenWidth = monitorInfo.rcWork.Right - monitorInfo.rcWork.Left;
                int screenHeight = monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top;

                //figure out the correct x position if the width is screenWidth / 3
                int newX = monitorInfo.rcWork.Left + (screenWidth * 3 / 4);

                int newY = monitorInfo.rcWork.Top;
                int newWidth = screenWidth / 4;
                int newHeight = screenHeight;

                // Set window position and size
                SetWindowPos(hwnd, HWND_TOPMOST, newX, newY, newWidth, newHeight, SWP_SHOWWINDOW);

                Debug.Log($"Docked window to right: X={newX}, Y={newY}, W={newWidth}, H={newHeight}");
            }
        }
        else
        {
            Debug.Log("Window docking runs only in the Windows build.");
        }
    }

    // New method to save current window position and dimensions
    public void SaveWindowPosition()
    {
        if (!Application.isEditor && hwnd != IntPtr.Zero)
        {
            RECT windowRect;
            if (GetWindowRect(hwnd, out windowRect))
            {
                int windowX = windowRect.Left;
                int windowY = windowRect.Top;
                int windowWidth = windowRect.Right - windowRect.Left;
                int windowHeight = windowRect.Bottom - windowRect.Top;

                // Store window information in PlayerPrefs
                PlayerPrefs.SetInt("windowX", windowX);
                PlayerPrefs.SetInt("windowY", windowY);
                PlayerPrefs.SetInt("windowWidth", windowWidth);
                PlayerPrefs.SetInt("windowHeight", windowHeight);
                PlayerPrefs.SetInt("isBorderless", isBorderless ? 1 : 0);
                PlayerPrefs.SetInt("windowOpacity", WindowOpacity);

                Debug.Log($"Window position saved: X={windowX}, Y={windowY}, W={windowWidth}, H={windowHeight}, Borderless={isBorderless}");
            }
        }
    }

    // New method to restore window position and dimensions
    private void RestoreWindowPosition()
    {
        if (PlayerPrefs.HasKey("calibrationComplete") && PlayerPrefs.HasKey("windowX"))
        {
            int windowX = PlayerPrefs.GetInt("windowX");
            int windowY = PlayerPrefs.GetInt("windowY");
            int windowWidth = PlayerPrefs.GetInt("windowWidth");
            int windowHeight = PlayerPrefs.GetInt("windowHeight");
            bool wasBorderless = PlayerPrefs.GetInt("isBorderless", 0) == 1;
            int savedOpacity = PlayerPrefs.GetInt("windowOpacity", 255);

            // Restore window opacity setting
            WindowOpacity = savedOpacity;

            // Restore borderless state
            isBorderless = wasBorderless;

            if (isBorderless)
            {
                // Set to borderless mode with transparency
                SetWindowLong(hwnd, GWL_STYLE, WS_POPUP);
                // Enable layered window for transparency
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED);
            }
            else
            {
                // Keep bordered mode (already set in Start)
                SetWindowLong(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) & ~WS_EX_LAYERED);
            }

            // Set window position and size
            SetWindowPos(hwnd, HWND_TOPMOST, windowX, windowY, windowWidth, windowHeight, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            // Apply transparency if in borderless mode
            if (isBorderless)
            {
                SetLayeredWindowAttributes(hwnd, 0, (byte)WindowOpacity, LWA_COLORKEY | LWA_ALPHA);
            }

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            Debug.Log($"Window position restored: X={windowX}, Y={windowY}, W={windowWidth}, H={windowHeight}, Borderless={isBorderless}");
        }
    }
}