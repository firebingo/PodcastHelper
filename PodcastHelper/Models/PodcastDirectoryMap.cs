using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.ServiceModel.Syndication;
using PodcastHelper.Helpers;
using System.Linq;
using PodcastHelper.Function;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace PodcastHelper.Models
{
	public class PodcastDirectoryMap
	{
		public Dictionary<string, PodcastDirectory> Podcasts { get; set; } = new Dictionary<string, PodcastDirectory>();

		public void CreateEmptyIfNone()
		{
			if (Podcasts.Count == 0)
				Podcasts.Add("null", new PodcastDirectory());
		}
	}

	public class PodcastDirectory
	{
		public string ShortCode { get; set; } = "null";
		public List<string> Names { get; set; } = new List<string>() { "Null Podcast" };
		public string FolderPath { get; set; } = "null";
		public string RssPath { get; set; } = string.Empty;
		public int MinEpisodeCount { get; set; } = 0;
		public int MaxEpisodeCount { get; set; } = int.MaxValue;
		public int LatestEpisode { get; set; } = 0;
		public int LastPlayed { get; set; } = 0;
		//private bool _hasLatest;
		private SyndicationFeed _feedCache = null;
		private ConcurrentDictionary<int, PodcastEpisode> _episodes = null;

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

		public void CheckListLoaded()
		{
			if (_episodes == null)
			{
				if (!Config.Instance.EpisodeList.Episodes.ContainsKey(ShortCode))
					Config.Instance.EpisodeList.Episodes.Add(ShortCode, new ConcurrentDictionary<int, PodcastEpisode>());
				_episodes = Config.Instance.EpisodeList.Episodes[ShortCode];
			}
		}

		public Task<int> CheckForNew()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(RssPath))
					return Task.FromResult(LatestEpisode);

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

				return Task.FromResult(LatestEpisode);
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
				return Task.FromResult(-1);
			}
		}

		public Task CheckForDownloadedEpisodes()
		{
			try
			{
				var files = GetRootAndOneSubFiles(Path.Combine(Config.Instance.ConfigObject.RootPath, FolderPath));
				foreach (var ep in _episodes)
				{
					if (files.Any(x => Path.GetFileName(x) == ep.Value.FileName))
						ep.Value.IsDownloaded = true;
					else
						ep.Value.IsDownloaded = false;
				}
			}
			catch (Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
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

			Parallel.ForEach(_feedCache.Items, (f) =>
			{
				var num = HelperMethods.GetEpisodeNumberFromFeed(f);
				//Kinda sucks but I havn't come up with a better way to handle these odd episodes at the moment
				if (num == -1)
					return;

				//I alsways need to read this url and update it since the url to download can change.
				Uri enclosure = null;
				if (f.Links != null)
					enclosure = f.Links.FirstOrDefault(x => x.RelationshipType.ToLowerInvariant() == "enclosure")?.Uri;

				if (!_episodes.ContainsKey(num) && num != -1 && num >= MinEpisodeCount && num <= MaxEpisodeCount)
				{
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
						Title = f.Title?.Text ?? string.Empty,
						Description = f.Summary?.Text ?? string.Empty,
						PublishDateUtc = f.PublishDate.UtcDateTime,
						Keywords = keywords?.ToArray() ?? Array.Empty<string>()
					};

					if (enclosure != null)
						episode.FileUri = enclosure;

					episode.Progress.Length = duration;
					_episodes.TryAdd(num, episode);
					addedNew = true;
				}
				else if (enclosure != null)
					_episodes[num].FileUri = enclosure;
			});

			if (addedNew)
				Config.Instance.SaveConfig();
		}

		public bool DownloadEpisode(int episode)
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
				return true;
			}
			return false;
		}

		private void OnFinishDownloading(bool res, int ep, string shortCode)
		{
			if (ShortCode != shortCode)
				return;
			var episodeToUse = _episodes[ep];
			episodeToUse.IsDownloaded = res;
			Config.Instance.SaveConfig();
			FileDownloader.OnDownloadFinishedEvent -= OnFinishDownloading;
			PodcastFunctions.UpdateLatestPodcastList();
		}

		private Task GetFeed()
		{
			try
			{
				var reader = XmlReader.Create(RssPath);
				var feed = SyndicationFeed.Load(reader);
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

		private static string[] GetRootAndOneSubFiles(string path)
		{
			var retval = new List<string>();
			Directory.CreateDirectory(path);
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
		public Dictionary<string, ConcurrentDictionary<int, PodcastEpisode>> Episodes { get; set; } = new Dictionary<string, ConcurrentDictionary<int, PodcastEpisode>>();
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
			Keywords = Array.Empty<string>();
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
