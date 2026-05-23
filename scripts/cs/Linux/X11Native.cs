using System;
using System.Runtime.InteropServices;
using Godot;

internal static class X11Native
{
	private const string LibX11   = "libX11.so.6";
	private const string LibXext  = "libXext.so.6";

	[StructLayout(LayoutKind.Sequential)]
	public struct XWindowAttributes
	{
		public int x, y;
		public int width, height;
		public int border_width;
		public int depth;
		public IntPtr visual;
		public IntPtr root;
		public int class_;
		public int bit_gravity;
		public int win_gravity;
		public int backing_store;
		public ulong backing_planes;
		public ulong backing_pixel;
		public int save_under;
		public IntPtr colormap;
		public int map_installed;
		public int map_state;
		public long all_event_masks;
		public long your_event_mask;
		public long do_not_propagate_mask;
		public int override_redirect;
		public IntPtr screen;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XClientMessageEvent
	{
		public int type;
		public ulong serial;
		public int send_event;
		public IntPtr display;
		public IntPtr window;
		public IntPtr message_type;
		public int format;
		public long data0;
		public long data1;
		public long data2;
		public long data3;
		public long data4;
	}

	[StructLayout(LayoutKind.Explicit, Size = 192)]
	public struct XEvent
	{
		[FieldOffset(0)] public int type;
		[FieldOffset(0)] public XClientMessageEvent xclient;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XRectangle
	{
		public short x;
		public short y;
		public ushort width;
		public ushort height;
	}

	public const int ClientMessage = 33;
	public const int PropertyChangeMask = 1 << 22;
	public const int SubstructureNotifyMask = 1 << 19;
	public const int SubstructureRedirectMask = 1 << 20;

	public const int _NET_WM_STATE_REMOVE = 0;
	public const int _NET_WM_STATE_ADD = 1;
	public const int _NET_WM_STATE_TOGGLE = 2;

	public const int ShapeBounding = 0;
	public const int ShapeClip = 1;
	public const int ShapeInput = 2;
	public const int ShapeSet = 0;

	public const int XA_WINDOW = 33;
	public const int XA_ATOM = 4;

	[DllImport(LibX11)] public static extern IntPtr XOpenDisplay(IntPtr displayName);
	[DllImport(LibX11)] public static extern IntPtr XInternAtom(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string name, int onlyIfExists);
	[DllImport(LibX11)] public static extern IntPtr XDefaultRootWindow(IntPtr display);
	[DllImport(LibX11)] public static extern int XFlush(IntPtr display);
	[DllImport(LibX11)] public static extern int XFree(IntPtr data);
	[DllImport(LibX11)] public static extern int XSync(IntPtr display, int discard);

	[DllImport(LibX11)]
	public static extern int XGetWindowAttributes(IntPtr display, IntPtr window, out XWindowAttributes attrs);

	[DllImport(LibX11)]
	public static extern int XQueryPointer(IntPtr display, IntPtr window,
		out IntPtr rootReturn, out IntPtr childReturn,
		out int rootX, out int rootY,
		out int winX, out int winY,
		out uint mask);

	[DllImport(LibX11)]
	public static extern int XSendEvent(IntPtr display, IntPtr window, int propagate, long eventMask, ref XEvent eventSend);

	[DllImport(LibX11)]
	public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, IntPtr data, int nelements);

	[DllImport(LibX11, EntryPoint = "XGetWindowProperty")]
	public static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property,
		long longOffset, long longLength, int delete, IntPtr reqType,
		out IntPtr actualTypeReturn, out int actualFormatReturn,
		out ulong nitemsReturn, out ulong bytesAfterReturn, out IntPtr propReturn);

	[DllImport(LibXext)]
	public static extern void XShapeCombineRectangles(IntPtr display, IntPtr window, int destKind,
		int xOff, int yOff, IntPtr rectangles, int nRects, int op, int ordering);

	[DllImport(LibXext)]
	public static extern void XShapeCombineMask(IntPtr display, IntPtr window, int destKind,
		int xOff, int yOff, IntPtr mask, int op);

	public static IntPtr GetGodotDisplay()
	{
		long h = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle, 0);
		return new IntPtr(h);
	}

	public static IntPtr GetGodotWindow(int windowId = 0)
	{
		long h = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, windowId);
		return new IntPtr(h);
	}

	public static void SetClickThrough(IntPtr display, IntPtr window, bool clickThrough)
	{
		if (display == IntPtr.Zero || window == IntPtr.Zero) return;
		if (clickThrough)
		{
			XShapeCombineRectangles(display, window, ShapeInput, 0, 0, IntPtr.Zero, 0, ShapeSet, 0);
		}
		else
		{
			XShapeCombineMask(display, window, ShapeInput, 0, 0, IntPtr.Zero, ShapeSet);
		}
		XFlush(display);
	}

	public static void SendNetWmStateMessage(IntPtr display, IntPtr window, IntPtr state1, IntPtr state2, int action)
	{
		if (display == IntPtr.Zero || window == IntPtr.Zero) return;
		IntPtr netWmState = XInternAtom(display, "_NET_WM_STATE", 0);
		IntPtr root = XDefaultRootWindow(display);
		var ev = new XEvent
		{
			xclient = new XClientMessageEvent
			{
				type = ClientMessage,
				display = display,
				window = window,
				message_type = netWmState,
				format = 32,
				data0 = action,
				data1 = (long)state1,
				data2 = (long)state2,
				data3 = 1,
				data4 = 0,
			}
		};
		XSendEvent(display, root, 0, SubstructureNotifyMask | SubstructureRedirectMask, ref ev);
		XFlush(display);
	}

	public static IntPtr GetActiveWindow(IntPtr display)
	{
		if (display == IntPtr.Zero) return IntPtr.Zero;
		IntPtr root = XDefaultRootWindow(display);
		IntPtr atomActive = XInternAtom(display, "_NET_ACTIVE_WINDOW", 0);
		int status = XGetWindowProperty(display, root, atomActive, 0, 1, 0, new IntPtr(XA_WINDOW),
			out IntPtr _, out int _, out ulong nitems, out ulong _, out IntPtr propReturn);
		if (status != 0 || propReturn == IntPtr.Zero || nitems == 0) return IntPtr.Zero;
		IntPtr win = Marshal.ReadIntPtr(propReturn);
		XFree(propReturn);
		return win;
	}

	public static bool WindowHasFullscreenState(IntPtr display, IntPtr window)
	{
		if (display == IntPtr.Zero || window == IntPtr.Zero) return false;
		IntPtr atomState = XInternAtom(display, "_NET_WM_STATE", 0);
		IntPtr atomFullscreen = XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", 0);
		int status = XGetWindowProperty(display, window, atomState, 0, 32, 0, new IntPtr(XA_ATOM),
			out IntPtr _, out int _, out ulong nitems, out ulong _, out IntPtr propReturn);
		if (status != 0 || propReturn == IntPtr.Zero) return false;
		bool found = false;
		for (ulong i = 0; i < nitems; i++)
		{
			IntPtr atom = Marshal.ReadIntPtr(propReturn, (int)i * IntPtr.Size);
			if (atom == atomFullscreen) { found = true; break; }
		}
		XFree(propReturn);
		return found;
	}
}
