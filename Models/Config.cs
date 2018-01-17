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

		public readonly string ConfigPath = @"AppData/Config.json";
		public ConfigModel ConfigObject;

		public void loadConfig()
		{
			try
			{
				if (File.Exists(ConfigPath))
				{
					var serializerSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
					ConfigObject = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(ConfigPath), serializerSettings);
				}
				else
				{
					ConfigObject = new ConfigModel();
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
