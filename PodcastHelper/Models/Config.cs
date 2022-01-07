using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PodcastHelper.Models
{
	public class ConfigModel
	{
		public string RootPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Podcasts");
		public string VlcRootUrl { get; set; } = "http://localhost:8080/";
		public string VlcUsername { get; set; } = string.Empty;
		public string VlcPassword { get; set; } = string.Empty;
		public PodcastDirectoryMap PodcastMap { get; set; } = new PodcastDirectoryMap();
		public WINDOWPLACEMENT WindowPlacement { get; set; }
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
