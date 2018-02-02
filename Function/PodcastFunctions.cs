using PodcastHelper.Helpers;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class PodcastFunctions
	{
		private static Dictionary<string, PodcastEpisode> _latestPodcastCache;
		public delegate void OnUpdate();
		public static event OnUpdate UpdateLatestList;

		static PodcastFunctions()
		{
			_latestPodcastCache = new Dictionary<string, PodcastEpisode>();
		}

		public static Dictionary<string, PodcastEpisode> LatestPodcastList
		{
			get
			{
				return _latestPodcastCache;
			}
		}

		public static Task LoadLatestPodcastList()
		{
			_latestPodcastCache.Clear();
			var pMap = Config.Instance.ConfigObject.PodcastMap;
			foreach (var podcast in pMap.Podcasts)
			{
				try
				{
					_latestPodcastCache.Add(podcast.Value.PrimaryName, Config.Instance.EpisodeList.Episodes[podcast.Value.ShortCode][podcast.Value.LatestEpisode]);
				}
				catch { }
			}
			return Task.FromResult(0);
		}

		public static async Task UpdateLatestPodcastList()
		{
			await LoadLatestPodcastList();
			UpdateLatestList?.Invoke();
		}

		public static List<PodcastEpisodeView> SearchPodcasts(string searchString)
		{
			var result = new List<PodcastEpisodeView>();
			var config = Config.Instance;

			foreach(var podcast in config.EpisodeList.Episodes)
			{
				foreach(var episode in podcast.Value)
				{
					var contains = false;
					if (episode.Value.FileName.ContainsInvariant(searchString) || episode.Value.EpisodeNumber.ToString().ContainsInvariant(searchString) 
						|| episode.Value.Title.ContainsInvariant(searchString) || episode.Value.Description.ContainsInvariant(searchString))
						contains = true;

					foreach(var s in episode.Value.Keywords)
					{
						if(s.ContainsInvariant(searchString))
						{
							contains = true;
							break;
						}
					}

					if (contains)
					{
						if (config.ConfigObject.PodcastMap.Podcasts.ContainsKey(episode.Value.PodcastShortCode))
							result.Add(new PodcastEpisodeView(config.ConfigObject.PodcastMap.Podcasts[episode.Value.PodcastShortCode].PrimaryName, episode.Value));
					}
				}
			}

			return result;
		}

		public static async Task DownloadEpisode(int ep, PodcastDirectory podcast)
		{
			await podcast.DownloadEpisode(ep);
		}

		public static async Task PlayFile(PodcastEpisodeView ep, bool fromStart = false)
		{
			var podcast = Config.Instance.ConfigObject.PodcastMap.Podcasts.FirstOrDefault(x => x.Value.PrimaryName == ep.PrimaryName).Value;
			if (podcast == null)
				return;
			var path = System.IO.Path.Combine(Config.Instance.ConfigObject.RootPath, podcast.FolderPath, ep.Episode.PublishDateUtc.Year.ToString(), ep.Episode.FileName);
			await VlcApi.PlayFile(path);
		}
	}
}
