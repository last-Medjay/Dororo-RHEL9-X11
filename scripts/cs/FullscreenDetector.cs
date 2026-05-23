using Godot;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public partial class FullscreenDetector : Node
{
	// ---- Win32 ----
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

	private IntPtr _display;
	private IntPtr _ownWindow;

	public override void _Ready()
	{
		if (OperatingSystem.IsLinux())
		{
			_display = X11Native.GetGodotDisplay();
			_ownWindow = X11Native.GetGodotWindow(0);
		}
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
			if (active == IntPtr.Zero || active == _ownWindow) return false;
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
