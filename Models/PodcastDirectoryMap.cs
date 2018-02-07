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
		public int LastPlayed { get; set; }
		//private bool _hasLatest;
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
			LastPlayed = 0;
			//_hasLatest = false;
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

				foreach (var ep in _episodes)
				{
					if (ep.Value.EpisodeNumber > newestEpisode)
						newestEpisode = ep.Value.EpisodeNumber;
				}

				if (newestEpisode > LatestEpisode)
					LatestEpisode = newestEpisode;

				Config.Instance.SaveConfig();

				return LatestEpisode;
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
				return -1;
			}
		}

		public Task CheckForDownloadedEpisodes()
		{
			var files = GetRootAndOneSubFiles(Path.Combine(Config.Instance.ConfigObject.RootPath, FolderPath));
			foreach (var ep in _episodes)
			{
				if (files.Any(x => Path.GetFileName(x) == ep.Value.FileName))
					ep.Value.IsDownloaded = true;
				else
					ep.Value.IsDownloaded = false;
			}
			Config.Instance.SaveConfig();
			return Task.FromResult(0);
		}

		public async Task FillNewEpisodes()
		{
			if (_feedCache == null)
				await GetFeed();

			CheckListLoaded();

			var addedNew = false;
			foreach (var f in _feedCache.Items)
			{
				var num = HelperMethods.GetEpisodeNumberFromFeed(f);
				if (!_episodes.ContainsKey(num) && num != -1 && num >= MinEpisodeCount && num <= MaxEpisodeCount)
				{
					Uri enclosure = null;
					if (f.Links != null)
						enclosure = f.Links.FirstOrDefault(x => x.RelationshipType.ToLowerInvariant() == "enclosure")?.Uri;

					List<string> keywords = null;
					TimeSpan duration = new TimeSpan();
					foreach (var ext in f.ElementExtensions)
					{
						switch (ext.OuterName.ToLowerInvariant())
						{
							case "keywords":
								keywords = HelperMethods.ReadKeywords(ext);
								break;
							case "duration":
								duration = HelperMethods.ReadDuration(ext);
								break;
						}
					}

					var episode = new PodcastEpisode()
					{
						PodcastShortCode = ShortCode,
						EpisodeNumber = num,
						Title = f.Title?.Text,
						Description = f.Summary?.Text,
						PublishDateUtc = f.PublishDate.UtcDateTime,
						Keywords = keywords.ToArray()
					};
					if (enclosure != null)
						episode.FileUri = enclosure;
					episode.Progress.Length = duration;
					_episodes.Add(num, episode);
					addedNew = true;
				}
			}

			if (addedNew)
				Config.Instance.SaveConfig();
		}

		public Task<bool> DownloadEpisode(int episode)
		{
			var info = new FileDownloadInfo();
			if (_episodes.ContainsKey(episode))
			{
				var episodeToUse = _episodes[episode];
				episodeToUse.IsDownloaded = false;
				info.FileUri = episodeToUse.FileUri.ToString();
				info.FilePath = Path.Combine(Config.Instance.ConfigObject.RootPath, FolderPath, episodeToUse.PublishDateUtc.Year.ToString(), episodeToUse.FileName);
				info.EpNumber = episode;
				info.PodcastShortCode = ShortCode;
				FileDownloader.AddFile(info);
				FileDownloader.OnDownloadFinishedEvent += OnFinishDownloading;
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		private void OnFinishDownloading(bool res, int ep, string shortCode)
		{
			if (ShortCode != shortCode)
				return;
			var episodeToUse = _episodes[ep];
			episodeToUse.IsDownloaded = res;
			Config.Instance.SaveConfig();
			FileDownloader.OnDownloadFinishedEvent -= OnFinishDownloading;
			PodcastFunctions.UpdateLatestPodcastList().ConfigureAwait(false);
		}

		private Task GetFeed()
		{
			XmlReader reader = null;
			SyndicationFeed feed = null;
			try
			{
				reader = XmlReader.Create(RssPath);
				feed = SyndicationFeed.Load(reader);
				reader.Close();
				_feedCache = feed;
				return Task.FromResult(0);
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
				return Task.FromException(ex);
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
		public string PodcastShortCode { get; set; }
		public int EpisodeNumber { get; set; }
		public string Title { get; set; }
		public Uri FileUri { get; set; }
		public int WatchCount { get; set; }
		public bool IsDownloaded { get; set; }
		public string Description { get; set; }
		public DateTime PublishDateUtc { get; set; }
		public EpisodeProgress Progress { get; set; }
		public string[] Keywords { get; set; }

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
			PodcastShortCode = string.Empty;
			Title = string.Empty;
			EpisodeNumber = 0;
			Progress = new EpisodeProgress();
			WatchCount = 0;
			FileUri = null;
			IsDownloaded = false;
			PublishDateUtc = DateTime.MinValue;
			Keywords = new string[0];
		}
	}

	public class EpisodeProgress
	{
		public double Progress { get; set; }
		public TimeSpan Length { get; set; }

		[JsonIgnore]
		public TimeSpan ProgressTime
		{
			get
			{
				return new TimeSpan(Convert.ToInt64(Length.Ticks * Progress));
			}
		}

		[JsonIgnore]
		public bool IsAtStart
		{
			get
			{
				return Progress.Equals(0.0);
			}
		}

		public EpisodeProgress()
		{
			Progress = 0.0;
			Length = new TimeSpan(0);
		}
	}
}
