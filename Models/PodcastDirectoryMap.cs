using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.ServiceModel.Syndication;
using PodcastHelper.Helpers;
using System.Linq;

namespace PodcastHelper.Models
{
	public class PodcastDirectoryMap
	{
		public Dictionary<string, PodcastDirectory> Podcasts;

		public PodcastDirectoryMap()
		{
			Podcasts = new Dictionary<string, PodcastDirectory>();
		}

		public void CreateEmptyIfNone()
		{
			if (Podcasts.Count == 0)
				Podcasts.Add("null", new PodcastDirectory());
		}
	}

	public partial class PodcastDirectory
	{
		public string ShortCode;
		public List<string> Names;
		public string FolderPath;
		public string RssPath;
		public int MinEpisodeCount;
		public int MaxEpisodeCount;
		public int LatestEpisode;
		public Dictionary<int, PodcastEpisode> Episodes;
		private bool _hasLatest;
		private SyndicationFeed _feedCache;


		[JsonIgnore]
		public string PrimaryName
		{
			get
			{
				if (Names == null || Names.Count == 0 || Names[0] == null)
					return "NO_NAME";
				return Names[0];
			}
		}

		public PodcastDirectory()
		{
			Names = new List<string>() { "Null Podcast" };
			RssPath = string.Empty;
			ShortCode = "null";
			FolderPath = "null";
			MinEpisodeCount = 0;
			MaxEpisodeCount = int.MaxValue;
			LatestEpisode = 0;
			Episodes = new Dictionary<int, PodcastEpisode>();
			_hasLatest = false;
		}

		public async Task<int> CheckForNew()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(RssPath))
					return LatestEpisode;

				var newestEpisode = -1;
				var highestCurrent = -1;

				var pathToGet = Path.Combine(Config.Instance.ConfigObject.RootPath, FolderPath);
				if (!Directory.Exists(pathToGet))
					Directory.CreateDirectory(pathToGet);
				var files = GetRootAndOneSubFiles(pathToGet);

				foreach (var f in files)
				{
					var num = HelperMethods.ParseEpisodeNumber(Path.GetFileNameWithoutExtension(f));
					if (num > highestCurrent)
						highestCurrent = num;
					//If we have the file here mark it as downloaded since we are looping over the files anyways.
					if (Episodes.ContainsKey(num))
						Episodes[num].IsDownloaded = true;
				}

				await GetFeed();

				foreach (var f in _feedCache.Items)
				{
					var num = HelperMethods.ParseEpisodeNumber(f.Title.Text);
					if (num > newestEpisode && num <= MaxEpisodeCount)
						newestEpisode = num;
				}

				if (newestEpisode > highestCurrent)
				{
					LatestEpisode = newestEpisode;
					_hasLatest = false;
				}

				Config.Instance.SaveConfig();

				return LatestEpisode;
			}
			catch(Exception ex)
			{
				return -1;
			}
		}

		public async Task FillNewEpisodes()
		{
			if (_feedCache == null)
				await GetFeed();

			var addedNew = false;
			foreach (var f in _feedCache.Items)
			{
				var num = HelperMethods.ParseEpisodeNumber(f.Title.Text);
				Uri enclosure = null;
				if (f.Links != null)
				{
					enclosure = f.Links.FirstOrDefault(x => x.RelationshipType.ToLowerInvariant() == "enclosure")?.Uri;
				}
				if (num != -1 && num >= MinEpisodeCount)
				{
					if (!Episodes.ContainsKey(num))
					{
						var episode = new PodcastEpisode() { EpisodeNumber = num, PublishDateUtc = f.PublishDate.UtcDateTime };
						if (enclosure != null)
							episode.FileName = enclosure.Segments.Last();
						Episodes.Add(num, episode);
						addedNew = true;
					}
				}
			}

			if(addedNew)
				Config.Instance.SaveConfig();
		}

		private async Task GetFeed()
		{
			XmlReader reader = null;
			SyndicationFeed feed = null;
			try
			{
				reader = XmlReader.Create(RssPath);
				feed = SyndicationFeed.Load(reader);
				reader.Close();
				_feedCache = feed;
			}
			catch (Exception ex)
			{
				return;
			}
		}

		private string[] GetRootAndOneSubFiles(string path)
		{
			var retval = new List<string>();

			var subDirectories = Directory.GetDirectories(path);
			retval.AddRange(Directory.GetFiles(path));
			foreach(var d in subDirectories)
			{
				retval.AddRange(Directory.GetFiles(d));
			}

			return retval.ToArray();
		}
	}

	public class PodcastEpisode
	{
		public int EpisodeNumber;
		public string FileName;
		public int WatchCount;
		public bool IsDownloaded;
		public DateTime PublishDateUtc;
		public EpisodeProgress Progress;

		public PodcastEpisode()
		{
			EpisodeNumber = 0;
			Progress = new EpisodeProgress();
			WatchCount = 0;
			FileName = "";
			IsDownloaded = false;
			PublishDateUtc = DateTime.MinValue;
		}
	}

	public class EpisodeProgress
	{
		public double Progress;
		public TimeSpan Length;
		public bool HasStarted;

		public EpisodeProgress()
		{
			Progress = 0.0;
			Length = new TimeSpan(0);
			HasStarted = false;
		}
	}
}
