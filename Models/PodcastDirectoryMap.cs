using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.ServiceModel.Syndication;
using PodcastHelper.Helpers;
using System.Linq;
using System.Net;
using PodcastHelper.Function;

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

	public class PodcastDirectory
	{
		public string ShortCode { get; set; }
		public List<string> Names { get; set; }
		public string FolderPath { get; set; }
		public string RssPath { get; set; }
		public int MinEpisodeCount { get; set; }
		public int MaxEpisodeCount { get; set; }
		public int LatestEpisode { get; set; }
		private bool _hasLatest;
		private SyndicationFeed _feedCache;
		private Dictionary<int, PodcastEpisode> _episodes;

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
			_hasLatest = false;
			_episodes = null;
		}

		public void CheckListLoaded()
		{
			if (_episodes == null)
			{
				if (!Config.Instance.EpisodeList.Episodes.ContainsKey(ShortCode))
					Config.Instance.EpisodeList.Episodes.Add(ShortCode, new Dictionary<int, PodcastEpisode>());
				_episodes = Config.Instance.EpisodeList.Episodes[ShortCode];
			}
		}

		public async Task<int> CheckForNew()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(RssPath))
					return LatestEpisode;

				CheckListLoaded();

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
					if (_episodes.ContainsKey(num))
						_episodes[num].IsDownloaded = true;
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
			catch (Exception ex)
			{
				return -1;
			}
		}

		public async Task FillNewEpisodes()
		{
			if (_feedCache == null)
				await GetFeed();

			CheckListLoaded();

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
					if (!_episodes.ContainsKey(num))
					{
						var episode = new PodcastEpisode() { EpisodeNumber = num, PublishDateUtc = f.PublishDate.UtcDateTime };
						if (enclosure != null)
							episode.FileUri = enclosure;
						_episodes.Add(num, episode);
						addedNew = true;
					}
				}
			}

			if (addedNew)
				Config.Instance.SaveConfig();
		}

		public async Task<bool> DownloadEpisode(int episode)
		{
			var info = new FileDownloadInfo();
			if(_episodes.ContainsKey(episode))
			{
				var episodeToUse = _episodes[episode];
				episodeToUse.IsDownloaded = false;
				info.FileUri = episodeToUse.FileUri.ToString();
				info.FilePath = Path.Combine(Config.Instance.ConfigObject.RootPath, FolderPath, episodeToUse.PublishDateUtc.Year.ToString(), episodeToUse.FileName);
				FileDownloader.AddFile(info);
				return true;
			}
			return false;
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
			foreach (var d in subDirectories)
			{
				retval.AddRange(Directory.GetFiles(d));
			}

			return retval.ToArray();
		}
	}

	public class PodcastEpisodeList
	{
		//               short name        ep num  episode info
		public Dictionary<string, Dictionary<int, PodcastEpisode>> Episodes { get; set; }

		public PodcastEpisodeList()
		{
			Episodes = new Dictionary<string, Dictionary<int, PodcastEpisode>>();
		}
	}

	public class PodcastEpisode
	{
		public int EpisodeNumber { get; set; }
		public Uri FileUri { get; set; }
		public int WatchCount { get; set; }
		public bool IsDownloaded { get; set; }
		public DateTime PublishDateUtc { get; set; }
		public EpisodeProgress Progress { get; set; }

		public bool NotIsDownloaded
		{
			get { return !IsDownloaded; }
		}

		public string FileName
		{
			get
			{
				if (FileUri == null)
					return string.Empty;
				return FileUri.Segments.Last();
			}
		}

		public PodcastEpisode()
		{
			EpisodeNumber = 0;
			Progress = new EpisodeProgress();
			WatchCount = 0;
			FileUri = null;
			IsDownloaded = false;
			PublishDateUtc = DateTime.MinValue;
		}
	}

	public class EpisodeProgress
	{
		public double Progress { get; set; }
		public TimeSpan Length { get; set; }
		public bool HasStarted { get; set; }

		public EpisodeProgress()
		{
			Progress = 0.0;
			Length = new TimeSpan(0);
			HasStarted = false;
		}
	}
}
