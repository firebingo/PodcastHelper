using System;
using System.IO;

namespace PodcastHelper.Models
{
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
