using PodcastHelper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class PodcastFunctions
	{
		public static Dictionary<string, PodcastEpisode> _latestPodcastCache;

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
				_latestPodcastCache.Add(podcast.Value.PrimaryName, podcast.Value.Episodes[podcast.Value.LatestEpisode]);
			}
		}
	}
}
