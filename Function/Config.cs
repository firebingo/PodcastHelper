using Newtonsoft.Json;
using PodcastHelper.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;

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
				var serializerSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
				if (File.Exists(EpisodeListPath))
				{
					EpisodeList = JsonConvert.DeserializeObject<PodcastEpisodeList>(File.ReadAllText(EpisodeListPath), serializerSettings);
				}
				else
				{
					EpisodeList = new PodcastEpisodeList();
				}

				if (File.Exists(ConfigPath))
				{
					ConfigObject = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(ConfigPath), serializerSettings);
				}
				else
				{
					ConfigObject = new ConfigModel();
					ConfigObject.PodcastMap.CreateEmptyIfNone();
				}

				if (File.Exists(MomentsPath))
				{
					MomentsList = JsonConvert.DeserializeObject<MomentsConfig>(File.ReadAllText(MomentsPath), serializerSettings);
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
				using (StreamWriter writer = new StreamWriter(File.Create(ConfigPath)))
				{
					JsonSerializer serializer = new JsonSerializer()
					{
						Formatting = Formatting.Indented
					};
					serializer.Serialize(writer, ConfigObject);
				}

				using (StreamWriter writer = new StreamWriter(File.Create(EpisodeListPath)))
				{
					JsonSerializer serializer = new JsonSerializer()
					{
						Formatting = Formatting.Indented
					};
					serializer.Serialize(writer, EpisodeList);
				}

				using (StreamWriter writer = new StreamWriter(File.Create(MomentsPath)))
				{
					JsonSerializer serializer = new JsonSerializer()
					{
						Formatting = Formatting.Indented
					};
					serializer.Serialize(writer, MomentsList);
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
			WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
			GetWindowPlacement(windowHandle, out placement);

			Config.Instance.ConfigObject.WindowPlacement = placement;
			Config.Instance.SaveConfig();
		}
	}
}
