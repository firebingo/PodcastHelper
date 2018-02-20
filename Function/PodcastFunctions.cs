using PodcastHelper.Helpers;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastHelper.Function
{
	public static class PodcastFunctions
	{
		private static Dictionary<string, PodcastEpisode> _latestPodcastCache;
		public delegate void OnUpdate();
		public static event OnUpdate UpdateLatestListEvent;

		private static Dictionary<string, PodcastEpisode> _latestPlayedCache;
		public static event OnUpdate UpdateLatestPlayedListEvent;

		public static PlayingState PlayingState = PlayingState.Stopped;
		private static PodcastEpisode _playingEpisode = null;
		public delegate void playingEpisodeChanged(PodcastEpisode episode);
		public static event playingEpisodeChanged PlayingEpisodeChangedEvent;
		private static Thread _playingThread;
		private static bool _runThread;

		static PodcastFunctions()
		{
			_latestPodcastCache = new Dictionary<string, PodcastEpisode>();
			_latestPlayedCache = new Dictionary<string, PodcastEpisode>();
			_runThread = true;
			_playingThread = new Thread(RunPlayingThread);
			_playingThread.Name = "FunctionsPlaying";
			_playingThread.Start();
		}

		public static Dictionary<string, PodcastEpisode> LatestPodcastList
		{
			get
			{
				return _latestPodcastCache;
			}
		}

		public static Dictionary<string, PodcastEpisode> LatestPlayedList
		{
			get
			{
				return _latestPlayedCache;
			}
		}

		public static PodcastEpisode PlayingEpisode
		{
			get
			{
				return _playingEpisode;
			}
			set
			{
				_playingEpisode = value;
				PlayingEpisodeChangedEvent?.Invoke(_playingEpisode);
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
			UpdateLatestListEvent?.Invoke();
		}

		public static Task LoadLatestPlayedList()
		{
			_latestPlayedCache.Clear();
			var pMap = Config.Instance.ConfigObject.PodcastMap;
			var episodes = Config.Instance.EpisodeList.Episodes;
			foreach (var podcast in pMap.Podcasts)
			{
				if (podcast.Value.LastPlayed < 1 || !episodes.ContainsKey(podcast.Value.ShortCode) || !episodes[podcast.Value.ShortCode].ContainsKey(podcast.Value.LastPlayed))
					continue;
				var ep = episodes[podcast.Value.ShortCode][podcast.Value.LastPlayed];
				if (ep != null)
					_latestPlayedCache.Add(podcast.Value.PrimaryName, ep);
			}
			return Task.FromResult(0);
		}

		public static async Task UpdateLatestPlayedList()
		{
			await LoadLatestPlayedList();
			UpdateLatestPlayedListEvent?.Invoke();
		}

		public static List<PodcastEpisodeView> SearchPodcasts(string searchString)
		{
			var result = new List<PodcastEpisodeView>();
			var config = Config.Instance;

			foreach(var podcast in config.EpisodeList.Episodes)
			{
				var pod = Config.Instance.ConfigObject.PodcastMap.Podcasts[podcast.Key];
				var temp = new List<PodcastEpisodeView>();
				foreach (var episode in podcast.Value)
				{
					var contains = false;
					if (episode.Value.FileName.ContainsInvariant(searchString) || episode.Value.EpisodeNumber.ToString().ContainsInvariant(searchString) 
						|| episode.Value.Title.ContainsInvariant(searchString) || episode.Value.Description.ContainsInvariant(searchString) 
						|| pod.Names.Any(x => x.ContainsInvariant(searchString)))
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
							temp.Add(new PodcastEpisodeView(config.ConfigObject.PodcastMap.Podcasts[episode.Value.PodcastShortCode].PrimaryName, episode.Value));
					}
				}
				result.AddRange(temp.OrderByDescending(x => x.Episode.EpisodeNumber));
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
			var seconds = 0;
			if (!fromStart)
				seconds = Convert.ToInt32(ep.Episode.Progress.ProgressTime.TotalSeconds);

			await VlcApi.PlayFile(path, seconds);

			podcast.LastPlayed = ep.Episode.EpisodeNumber;
			await UpdateLatestPlayedList().ConfigureAwait(false);
		}

		public static async Task SeekFile(double value)
		{
			if (PlayingState == PlayingState.Stopped || PlayingEpisode == null)
				return;
			var seconds = PlayingEpisode.Progress.Length.TotalSeconds * (value / 100);
			await VlcApi.SeekFile(Convert.ToInt32(seconds));
			PlayingEpisode.Progress.Progress = value / 100;
			Config.Instance.SaveConfig();
		}

		public static async Task PauseCommand()
		{
			await VlcApi.PauseToggle();
			if (PlayingState == PlayingState.Paused)
				PlayingState = PlayingState.Playing;
			else if (PlayingState == PlayingState.Playing)
				PlayingState = PlayingState.Paused;
		}

		public static async Task StopCommand()
		{
			await VlcApi.StopFile();
			PlayingState = PlayingState.Stopped;
		}

		private static void RunPlayingThread()
		{
			do
			{
				Thread.Sleep(1000);
				if (PlayingState == PlayingState.Playing)
				{
					double addTime = 1 / PlayingEpisode.Progress.Length.TotalSeconds;
					PlayingEpisode.Progress.Progress += addTime;
					PlayingEpisodeChangedEvent?.Invoke(_playingEpisode);
				}
			} while (_runThread);

			return;
		}

		public static void Kill()
		{
			_runThread = false;
			_playingThread.Abort();
		}
	}
}
