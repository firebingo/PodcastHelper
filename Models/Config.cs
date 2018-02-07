using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PodcastHelper.Models
{
	public class ConfigModel
	{
		public string RootPath;
		public string VlcRootUrl;
		public string VlcUsername;
		public string VlcPassword;
		public PodcastDirectoryMap PodcastMap;
		public WINDOWPLACEMENT WindowPlacement;

		public ConfigModel()
		{
			RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Podcasts");
			VlcRootUrl = "http://localhost:8080/";
			VlcUsername = string.Empty;
			VlcPassword = string.Empty;
			PodcastMap = new PodcastDirectoryMap();
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct WINDOWPLACEMENT
	{
		public int length;
		public int flags;
		public int showCmd;
		public POINT minPosition;
		public POINT maxPosition;
		public RECT normalPosition;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT(int left, int top, int right, int bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}
	}

	// POINT structure required by WINDOWPLACEMENT structure
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int X;
		public int Y;

		public POINT(int x, int y)
		{
			X = x;
			Y = y;
		}
	}
}
