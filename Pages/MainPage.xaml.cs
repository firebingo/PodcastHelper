using PodcastHelper.Function;
using PodcastHelper.Models;
using PodcastHelper.Resources;
using PodcastHelper.Windows;
using System;
using System.Collections.Generic;
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
		private RecentPlayedListData recentPlayedListData = null;
		private ErrorData errorData = null;
		private SearchPodcastData searchData = null;
		private TimeSliderData sliderData = null;

		public MainPage()
		{
			InitializeComponent();
			config = Config.Instance;

			recentListData = new RecentPodcastListData();
			podcastListItems.DataContext = recentListData;
			recentPlayedListData = new RecentPlayedListData();
			podcastRecentPlayedItems.DataContext = recentPlayedListData;
			searchData = new SearchPodcastData();
			searchPodcastList.DataContext = searchData;
			errorData = new ErrorData();
			errorGrid.DataContext = errorData;
			errorData.Error = "Loading...";
			sliderData = new TimeSliderData();
			timeSlider.DataContext = sliderData;
			PodcastFunctions.UpdateLatestListEvent += OnLatestListUpdate;
			PodcastFunctions.UpdateLatestPlayedListEvent += OnRecentPlayListUpdate;
			ItemsControlTemplates.OnDownloadRecentEvent += DownloadRecentClicked;
			ItemsControlTemplates.OnSelectEpisodeEvent += SelectEpisodeClicked;
			ItemsControlTemplates.OnPlayEpisodeEvent += PlayRecentClicked;
			MainWindow.OnMainWindowSizeChanged += OnMainWindowSizeChanged;
			Task.Run(() => InitializePodcasts());
		}

		public async Task InitializePodcasts()
		{
			foreach (var pod in config.ConfigObject.PodcastMap.Podcasts)
			{
				await pod.Value.CheckForNew();
				await pod.Value.FillNewEpisodes();
				await pod.Value.CheckForDownloadedEpisodes();
			}
			await PodcastFunctions.UpdateLatestPodcastList().ConfigureAwait(false);
			await PodcastFunctions.UpdateLatestPlayedList().ConfigureAwait(false);
			errorData.Error = "";
			VlcApi.DoNothing();
		}

		private void OnMainWindowSizeChanged(double width, double height)
		{
			sliderData.Width = Convert.ToInt32(width - 40);
		}

		private void OnLatestListUpdate()
		{
			recentListData.UpdateRecentList(PodcastFunctions.LatestPodcastList);
		}

		private void OnRecentPlayListUpdate()
		{
			recentPlayedListData.UpdateRecentList(PodcastFunctions.LatestPlayedList);
		}

		private void DownloadRecentClicked(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.DataContext is PodcastEpisodeView kvp)
			{
				var podcast = config.ConfigObject.PodcastMap.Podcasts.FirstOrDefault(x => x.Value.PrimaryName == kvp.PrimaryName).Value;
				var ep = kvp.Episode.EpisodeNumber;
				if (podcast == null)
					return;
				PodcastFunctions.DownloadEpisode(ep, podcast).ConfigureAwait(false);
			}
		}

		private void PlayRecentClicked(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.DataContext is PodcastEpisodeView kvp)
				PodcastFunctions.PlayFile(kvp, false).ConfigureAwait(false);
		}

		private void SearchPodcastsClicked(object sender, RoutedEventArgs e)
		{
			var res = PodcastFunctions.SearchPodcasts(searchData.SearchString);
			searchData.SearchResults.Clear();
			searchData.SearchResults = res;
		}

		private void SearchPodcastKeyUp(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter && sender is TextBox textBox)
			{
				var res = PodcastFunctions.SearchPodcasts(textBox.Text);
				searchData.SearchResults.Clear();
				searchData.SearchResults = res;
			}
		}

		private void CloseSearchSummary(object sender, RoutedEventArgs e)
		{
			searchData.CurrentEpisode = null;
		}

		public void SummaryDownloadClicked(object sender, RoutedEventArgs e)
		{
			DownloadRecentClicked(sender, e);
		}

		public void SelectEpisodeClicked(object sender, RoutedEventArgs e)
		{
			if ((sender as Grid)?.DataContext is PodcastEpisodeView episode)
				searchData.CurrentEpisode = episode;
		}

		private void SummaryPlayClicked(object sender, RoutedEventArgs e)
		{
			if((sender as Button)?.DataContext is PodcastEpisodeView kvp)
				PodcastFunctions.PlayFile(kvp, false).ConfigureAwait(false);
		}

		private void SummaryPlayStartClicked(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.DataContext is PodcastEpisodeView kvp)
				PodcastFunctions.PlayFile(kvp, true).ConfigureAwait(false);
		}

		private void TimeSliderMouseUp(object sender, MouseButtonEventArgs e)
		{
			if(sender is Slider slider)
				PodcastFunctions.SeekFile(slider.Value).ConfigureAwait(false);
		}

		private void PlayClicked(object sender, RoutedEventArgs e)
		{
			PodcastFunctions.PauseCommand().ConfigureAwait(false);
		}

		private void StopClicked(object sender, RoutedEventArgs e)
		{
			PodcastFunctions.StopCommand().ConfigureAwait(false);
		}
	}
}
