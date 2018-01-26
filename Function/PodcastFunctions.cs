using PodcastHelper.Helpers;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		public static async Task LoadLatestPodcastList()
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
		}

		public static async Task UpdateLatestPodcastList()
		{
			await LoadLatestPodcastList();
			UpdateLatestList?.Invoke();
		}

		public static ObservableCollection<PodcastEpisodeView> SearchPodcasts(string searchString)
		{
			var result = new ObservableCollection<PodcastEpisodeView>();
			var config = Config.Instance;

			foreach(var podcast in config.EpisodeList.Episodes)
			{
				foreach(var episode in podcast.Value)
				{
					if(episode.Value.FileName.ContainsInvariant(searchString) || episode.Value.EpisodeNumber.ToString().ContainsInvariant(searchString) || episode.Value.Title.ContainsInvariant(searchString))
					{
						if(config.ConfigObject.PodcastMap.Podcasts.ContainsKey(episode.Value.PodcastShortCode))
							result.Add(new PodcastEpisodeView(config.ConfigObject.PodcastMap.Podcasts[episode.Value.PodcastShortCode].PrimaryName, episode.Value));
					}
				}
			}

			return result;
		}
	}
}
