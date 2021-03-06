﻿using PodcastHelper.Function;
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
		private OtherMainPageData otherPageData = null;
		private bool isLoading = false;

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
			otherPageData = new OtherMainPageData();
			AlbumArt.DataContext = otherPageData;
			PodcastFunctions.UpdateLatestListEvent += OnLatestListUpdate;
			PodcastFunctions.UpdateLatestPlayedListEvent += OnRecentPlayListUpdate;
			PodcastFunctions.AlbumArtUpdated += OnAlbumArtUpdated;
			ItemsControlTemplates.OnDownloadRecentEvent += DownloadRecentClicked;
			ItemsControlTemplates.OnSelectEpisodeEvent += SelectEpisodeClicked;
			ItemsControlTemplates.OnPlayEpisodeEvent += PlayRecentClicked;
			MainWindow.OnMainWindowSizeChanged += OnMainWindowSizeChanged;
			Task.Run(() => InitializePodcasts());
		}

		public async Task InitializePodcasts()
		{
			isLoading = true;
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
			isLoading = false;
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

		private void OnAlbumArtUpdated(string uri)
		{
			if (string.IsNullOrWhiteSpace(uri))
				otherPageData.AlbumArt = null;
			else
			{
				var art = new BitmapImage();
				art.BeginInit();
				art.UriSource = new Uri(uri);
				art.DecodePixelHeight = 175;
				art.EndInit();
				art.Freeze();

				otherPageData.AlbumArt = art;
			}
		}

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			if (!isLoading)
			{
				errorData.Error = "Loading...";
				Task.Run(() => InitializePodcasts());
			}
		}

		private void MomentsClicked(object sender, RoutedEventArgs e)
		{
			if(Window.GetWindow(this) is MainWindow w)
				w.VisiblePage = VisiblePage.Moments;
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

		private void TimeSliderMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is Slider slider)
				sliderData.MovingTimeSlider = true;
		}

		private void TimeSliderMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is Slider slider)
			{
				sliderData.MovingTimeSlider = false;
				PodcastFunctions.SeekFile(slider.Value).ConfigureAwait(false);
			}
		}

		private void PlayClicked(object sender, RoutedEventArgs e)
		{
			PodcastFunctions.PauseCommand().ConfigureAwait(false);
		}

		private void StopClicked(object sender, RoutedEventArgs e)
		{
			PodcastFunctions.StopCommand().ConfigureAwait(false);
		}

		private void HorizontalScrollMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (sender is ScrollViewer scrollviewer)
			{
				if (e.Delta > 0)
					scrollviewer.LineLeft();
				else
					scrollviewer.LineRight();
				e.Handled = true;
			}
		}
	}
}
