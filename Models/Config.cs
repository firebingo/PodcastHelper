using System;
using System.IO;

namespace PodcastHelper.Models
{
	public class ConfigModel
	{
		public string RootPath;
		public string VlcRootUrl;
		public string VlcUsername;
		public string VlcPassword;
		public PodcastDirectoryMap PodcastMap;

		public ConfigModel()
		{
			RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Podcasts");
			VlcRootUrl = "http://localhost:8080/";
			VlcUsername = string.Empty;
			VlcPassword = string.Empty;
			PodcastMap = new PodcastDirectoryMap();
		}
	}
}
