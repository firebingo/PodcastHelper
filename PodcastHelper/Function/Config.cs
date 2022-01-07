using PodcastHelper.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PodcastHelper.Function
{
	public class Config
	{
		private static Config _instance;
		public static Config Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Config();
					_instance.LoadConfig();
				}
				return _instance;
			}
		}

		private readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
		private readonly string ConfigPath = @"AppData/Config.json";
		private readonly string EpisodeListPath = @"AppData/Episodes.json";
		private readonly string MomentsPath = @"AppData/Moments.json";
		public ConfigModel ConfigObject;
		public PodcastEpisodeList EpisodeList;
		public MomentsConfig MomentsList;

		public void LoadConfig()
		{
			try
			{
				if (File.Exists(EpisodeListPath))
				{
					EpisodeList = JsonSerializer.Deserialize<PodcastEpisodeList>(File.ReadAllText(EpisodeListPath));
				}
				else
				{
					EpisodeList = new PodcastEpisodeList();
				}

				if (File.Exists(ConfigPath))
				{
					ConfigObject = JsonSerializer.Deserialize<ConfigModel>(File.ReadAllText(ConfigPath));
				}
				else
				{
					ConfigObject = new ConfigModel();
					ConfigObject.PodcastMap.CreateEmptyIfNone();
				}

				if (File.Exists(MomentsPath))
				{
					MomentsList = JsonSerializer.Deserialize<MomentsConfig>(File.ReadAllText(MomentsPath));
				}
				else
				{
					MomentsList = new MomentsConfig();
				}
				SaveConfig();
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}

		public void SaveConfig()
		{
			try
			{
				string directoryName = Path.GetDirectoryName(ConfigPath);
				if (!Directory.Exists(directoryName))
					Directory.CreateDirectory(directoryName);
				using (var writer = File.Create(ConfigPath))
				{
					JsonSerializer.Serialize(writer, ConfigObject, _jsonSerializerOptions);
				}

				using (var writer = File.Create(EpisodeListPath))
				{
					JsonSerializer.Serialize(writer, EpisodeList, _jsonSerializerOptions);
				}

				using (var writer = File.Create(MomentsPath))
				{
					JsonSerializer.Serialize(writer, MomentsList, _jsonSerializerOptions);
				}
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}
	}

	public static class WindowPlacementHandler
	{
		[DllImport("user32.dll")]
		private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

		private const int SW_SHOWNORMAL = 1;
		private const int SW_SHOWMINIMIZED = 2;

		public static void SetPlacement(IntPtr windowHandle)
		{
			WINDOWPLACEMENT placement;

			try
			{
				placement = Config.Instance.ConfigObject.WindowPlacement;
				//If the top and bottom are both 0 it probably means the config file had it wiped and we want to go back to defaults.
				if (placement.normalPosition.Bottom == 0 && placement.normalPosition.Top == 0)
					return;

				placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				placement.flags = 0;
				placement.showCmd = (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd);
				SetWindowPlacement(windowHandle, ref placement);
			}
			catch (InvalidOperationException) { }
		}

		public static void GetPlacement(IntPtr windowHandle)
		{
			GetWindowPlacement(windowHandle, out var placement);

			Config.Instance.ConfigObject.WindowPlacement = placement;
			Config.Instance.SaveConfig();
		}
	}
}
