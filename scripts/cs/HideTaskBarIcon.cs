using Godot;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public partial class HideTaskBarIcon : Node
{
	// ---- Win32 ----
	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_APPWINDOW = 0x00040000;
	private const int WS_EX_TOOLWINDOW = 0x00000080;

	public async void HideIcon(int windowId)
	{
		// Window may not be mapped by the WM at _Ready time on either platform.
		await ToSignal(GetTree().CreateTimer(1), "timeout");

		if (OperatingSystem.IsWindows())
		{
			HideIconWindows(windowId);
		}
		else if (OperatingSystem.IsLinux())
		{
			HideIconX11(windowId);
		}
	}

	[SupportedOSPlatform("windows")]
	private void HideIconWindows(int windowId)
	{
		long windowHandle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, windowId);
		IntPtr handle = new IntPtr(windowHandle);

		int style = GetWindowLong(handle, GWL_EXSTYLE);
		style = style & ~WS_EX_APPWINDOW;
		style = style | WS_EX_TOOLWINDOW;
		SetWindowLong(handle, GWL_EXSTYLE, style);
	}

	private void HideIconX11(int windowId)
	{
		IntPtr display = X11Native.GetGodotDisplay();
		IntPtr window = X11Native.GetGodotWindow(windowId);
		if (display == IntPtr.Zero || window == IntPtr.Zero) return;

		IntPtr skipTaskbar = X11Native.XInternAtom(display, "_NET_WM_STATE_SKIP_TASKBAR", 0);
		IntPtr skipPager = X11Native.XInternAtom(display, "_NET_WM_STATE_SKIP_PAGER", 0);
		X11Native.SendNetWmStateMessage(display, window, skipTaskbar, skipPager, X11Native._NET_WM_STATE_ADD);
	}
}
