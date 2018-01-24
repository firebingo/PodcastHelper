using PodcastHelper.Models;
using System.Collections.Generic;
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
	}
}
