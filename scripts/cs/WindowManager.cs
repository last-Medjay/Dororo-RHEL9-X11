using Godot;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public partial class WindowManager : Node
{
	// ---- Win32 ----
	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	private struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	private const int GwlExStyle = -20;
	private const uint WsExLayered = 0x00080000;
	private const uint WsExTransparent = 0x00000020;
	private const int WS_EX_APPWINDOW = 0x00040000;
	private const int WS_EX_TOOLWINDOW = 0x00000080;

	private IntPtr _hWnd;
	private IntPtr _display;   // X11 only

	public override void _Ready()
	{
		// Use Godot's native handle on both platforms — GetActiveWindow() races startup focus.
		_hWnd = new IntPtr(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, 0));

		if (OperatingSystem.IsWindows())
		{
			InitializeWindowStyleWindows();
		}
		else if (OperatingSystem.IsLinux())
		{
			_display = X11Native.GetGodotDisplay();
		}
	}

	[SupportedOSPlatform("windows")]
	private void InitializeWindowStyleWindows()
	{
		int currentStyle = GetWindowLong(_hWnd, GwlExStyle);
		int newStyle = currentStyle | (int)WsExLayered;
		SetWindowLong(_hWnd, GwlExStyle, newStyle);
	}

	public void SetClickThrough(bool clickthrough)
	{
		if (_hWnd == IntPtr.Zero) return;

		if (OperatingSystem.IsWindows())
		{
			SetClickThroughWindows(clickthrough);
		}
		else if (OperatingSystem.IsLinux())
		{
			X11Native.SetClickThrough(_display, _hWnd, clickthrough);
		}
	}

	[SupportedOSPlatform("windows")]
	private void SetClickThroughWindows(bool clickthrough)
	{
		int currentStyle = GetWindowLong(_hWnd, GwlExStyle);
		currentStyle = currentStyle & ~((int)WsExLayered | (int)WsExTransparent);

		if (clickthrough)
		{
			currentStyle = currentStyle | (int)(WsExLayered | WsExTransparent);
		}
		else
		{
			currentStyle = currentStyle | (int)WsExLayered;
		}

		currentStyle = currentStyle | WS_EX_TOOLWINDOW;
		currentStyle = currentStyle & ~WS_EX_APPWINDOW;

		SetWindowLong(_hWnd, GwlExStyle, currentStyle);
	}

	public void HideTaskbarIcon()
	{
		if (_hWnd == IntPtr.Zero) return;

		if (OperatingSystem.IsWindows())
		{
			HideTaskbarIconWindows();
		}
		else if (OperatingSystem.IsLinux())
		{
			IntPtr skipTaskbar = X11Native.XInternAtom(_display, "_NET_WM_STATE_SKIP_TASKBAR", 0);
			IntPtr skipPager = X11Native.XInternAtom(_display, "_NET_WM_STATE_SKIP_PAGER", 0);
			X11Native.SendNetWmStateMessage(_display, _hWnd, skipTaskbar, skipPager, X11Native._NET_WM_STATE_ADD);
		}
	}

	[SupportedOSPlatform("windows")]
	private void HideTaskbarIconWindows()
	{
		int currentStyle = GetWindowLong(_hWnd, GwlExStyle);
		currentStyle = currentStyle & ~WS_EX_APPWINDOW;
		currentStyle = currentStyle | WS_EX_TOOLWINDOW;
		SetWindowLong(_hWnd, GwlExStyle, currentStyle);
	}

	public bool IsOtherAppFullscreen()
	{
		if (OperatingSystem.IsWindows())
		{
			return IsOtherAppFullscreenWindows();
		}
		else if (OperatingSystem.IsLinux())
		{
			IntPtr active = X11Native.GetActiveWindow(_display);
			if (active == IntPtr.Zero || active == _hWnd) return false;
			return X11Native.WindowHasFullscreenState(_display, active);
		}
		return false;
	}

	[SupportedOSPlatform("windows")]
	private bool IsOtherAppFullscreenWindows()
	{
		IntPtr hWnd = GetForegroundWindow();
		RECT rect;
		if (hWnd != IntPtr.Zero && GetWindowRect(hWnd, out rect))
		{
			int windowWidth = rect.Right - rect.Left;
			int windowHeight = rect.Bottom - rect.Top;

			Godot.Vector2I screenSize = DisplayServer.ScreenGetSize();

			return windowWidth == screenSize.X && windowHeight == screenSize.Y;
		}
		else
		{
			return false;
		}
	}
}
