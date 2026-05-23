using Godot;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public partial class MouseTracker : Node
{
	// ---- Win32 ----
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int X;
		public int Y;
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	private IntPtr _display;

	public override void _Ready()
	{
		if (OperatingSystem.IsLinux())
		{
			_display = X11Native.GetGodotDisplay();
		}
	}

	public Vector2I GetMousePosition()
	{
		if (OperatingSystem.IsWindows())
		{
			return GetMousePositionWindows();
		}
		else if (OperatingSystem.IsLinux())
		{
			return GetMousePositionX11();
		}
		return Vector2I.Zero;
	}

	public Vector2I GetMousePositionGlobal()
	{
		if (OperatingSystem.IsWindows())
		{
			return GetMousePositionGlobalWindows();
		}
		else if (OperatingSystem.IsLinux())
		{
			// On X11 the root window already spans the virtual screen at (0,0).
			return GetMousePositionX11();
		}
		return Vector2I.Zero;
	}

	[SupportedOSPlatform("windows")]
	private Vector2I GetMousePositionWindows()
	{
		if (GetCursorPos(out POINT point))
		{
			return new Vector2I(point.X, point.Y);
		}
		return Vector2I.Zero;
	}

	[SupportedOSPlatform("windows")]
	private Vector2I GetMousePositionGlobalWindows()
	{
		if (GetCursorPos(out POINT point))
		{
			int screenLeft = GetSystemMetrics(76);
			int screenTop = GetSystemMetrics(77);
			return new Vector2I(point.X - screenLeft, point.Y - screenTop);
		}
		return Vector2I.Zero;
	}

	private Vector2I GetMousePositionX11()
	{
		if (_display == IntPtr.Zero) return Vector2I.Zero;
		IntPtr root = X11Native.XDefaultRootWindow(_display);
		int status = X11Native.XQueryPointer(_display, root,
			out IntPtr _, out IntPtr _,
			out int rootX, out int rootY,
			out int _, out int _,
			out uint _);
		if (status == 0) return Vector2I.Zero;
		return new Vector2I(rootX, rootY);
	}
}
