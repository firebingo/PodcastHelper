using Newtonsoft.Json;
using PodcastHelper.Models;
using System;
using System.IO;

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
				SaveConfig();
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
}
