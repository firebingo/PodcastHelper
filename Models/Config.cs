using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PodcastHelper.Models
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
					_instance.loadConfig();
				}
				return _instance;
			}
		}

		private readonly string ConfigPath = @"AppData/Config.json";
		private readonly string EpisodeListPath = @"AppData/Episodes.json";
		public ConfigModel ConfigObject;
		public PodcastEpisodeList EpisodeList;

		public void loadConfig()
		{
			try
			{
				if (File.Exists(ConfigPath))
				{
					var serializerSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
					EpisodeList = JsonConvert.DeserializeObject<PodcastEpisodeList>(File.ReadAllText(EpisodeListPath), serializerSettings);
					ConfigObject = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(ConfigPath), serializerSettings);
				}
				else
				{
					ConfigObject = new ConfigModel();
					EpisodeList = new PodcastEpisodeList();
					ConfigObject.PodcastMap.CreateEmptyIfNone();
					SaveConfig();
				}
			}
			catch (Exception ex)
			{

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
					JsonSerializer serializer = new JsonSerializer();
					serializer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, ConfigObject);
				}

				using (StreamWriter writer = new StreamWriter(File.Create(EpisodeListPath)))
				{
					JsonSerializer serializer = new JsonSerializer();
					serializer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, EpisodeList);
				}
			}
			catch (Exception ex)
			{

			}
		}
	}

	public class ConfigModel
	{
		public string RootPath;
		public PodcastDirectoryMap PodcastMap;

		public ConfigModel()
		{
			RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Podcasts");
			PodcastMap = new PodcastDirectoryMap();
		}
	}
}
