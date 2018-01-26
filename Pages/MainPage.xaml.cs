using PodcastHelper.Function;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PodcastHelper.Pages
{
	/// <summary>
	/// Interaction logic for MainPage.xaml
	/// </summary>
	public partial class MainPage : Page
	{
		public Config config;
		private RecentPodcastListData recentListData = null;
		private ErrorData errorData = null;
		private SearchPodcastData searchData = null;

		public MainPage()
		{
			InitializeComponent();
			config = Config.Instance;

			errorData = new ErrorData();
			errorGrid.DataContext = errorData;
			ErrorTracker.CurrentError = "test error";
			PodcastFunctions.UpdateLatestList += OnLatestListUpdate;
			initializePodcasts().ConfigureAwait(false);
		}

		public async Task initializePodcasts()
		{
			recentListData = new RecentPodcastListData();
			recentPodcastList.DataContext = recentListData;
			searchData = new SearchPodcastData();
			searchPodcastList.DataContext = searchData;

			foreach (var pod in config.ConfigObject.PodcastMap.Podcasts)
			{
				await pod.Value.CheckForNew();
				await pod.Value.FillNewEpisodes();
				await pod.Value.CheckForDownloadedEpisodes();
			}
			PodcastFunctions.UpdateLatestPodcastList();
		}

		private void OnLatestListUpdate()
		{
			recentListData.UpdateRecentList(PodcastFunctions.LatestPodcastList);
		}

		private void DownloadRecentClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button?.DataContext is PodcastEpisodeView)
			{
				var kvp = ((PodcastEpisodeView)button.DataContext);
				var podcast = config.ConfigObject.PodcastMap.Podcasts.FirstOrDefault(x => x.Value.PrimaryName == kvp.PrimaryName).Value;
				var ep = kvp.Episode.EpisodeNumber;
				if (podcast == null)
					return;
				DownloadEpisode(ep, podcast).ConfigureAwait(false);
			}
			else
				return;
		}

		private void SearchPodcastsClicked(object sender, RoutedEventArgs e)
		{
			var res = PodcastFunctions.SearchPodcasts(searchData.SearchString);
			searchData.SearchResults.Clear();
			searchData.SearchResults = res;
		}

		public void SelectEpisodeClicked(object sender, RoutedEventArgs e)
		{
			var grid = sender as Grid;
			if(grid != null)
			{
				var episode = grid.DataContext as PodcastEpisodeView;
				if (episode != null)
					searchData.CurrentEpisode = episode;
			}
		}

		private async Task DownloadEpisode(int ep, PodcastDirectory podcast)
		{
			await podcast.DownloadEpisode(ep);
		}
	}
}
