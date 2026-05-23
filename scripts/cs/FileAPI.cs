using Godot;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public partial class FileAPI : Node
{
	// ---- Win32 ----
	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SHFILEOPSTRUCT
	{
		public IntPtr hwnd;
		public uint wFunc;
		public string pFrom;
		public string pTo;
		public ushort fFlags;
		public bool fAnyOperationsAborted;
		public IntPtr hNameMappings;
		public string lpszProgressTitle;
	}

	private const uint FO_DELETE = 0x0003;
	private const ushort FOF_ALLOWUNDO = 0x0040;
	private const ushort FOF_NOCONFIRMATION = 0x0010;

	public void MoveFileToRecycleBin(string filePath)
	{
		if (OperatingSystem.IsWindows())
		{
			MoveFileToRecycleBinWindows(filePath);
		}
		else if (OperatingSystem.IsLinux())
		{
			MoveFileToTrashLinux(filePath);
		}
	}

	[SupportedOSPlatform("windows")]
	private void MoveFileToRecycleBinWindows(string filePath)
	{
		var fileOp = new SHFILEOPSTRUCT
		{
			wFunc = FO_DELETE,
			pFrom = filePath + '\0'.ToString(),
			fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION,
		};
		SHFileOperation(ref fileOp);
	}

	private void MoveFileToTrashLinux(string filePath)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "gio",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			psi.ArgumentList.Add("trash");
			psi.ArgumentList.Add(filePath);

			using var proc = Process.Start(psi);
			if (proc == null)
			{
				GD.PushError("FileAPI: failed to start `gio trash`");
				return;
			}
			proc.WaitForExit(5000);
			if (proc.ExitCode != 0)
			{
				GD.PushError($"FileAPI: gio trash exited {proc.ExitCode} for `{filePath}`");
			}
		}
		catch (Exception e)
		{
			GD.PushError($"FileAPI: gio trash failed: {e.Message}");
		}
	}
}
