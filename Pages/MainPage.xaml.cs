using PodcastHelper.Function;
using PodcastHelper.Models;
using System;
using System.Collections.Generic;
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

		public MainPage()
		{
			InitializeComponent();
			config = Config.Instance;

			intilizePodcasts().ConfigureAwait(false);
		}

		public async Task intilizePodcasts()
		{
			recentListData = new RecentPodcastListData();
			recentPodcastList.DataContext = recentListData;
			foreach (var pod in config.ConfigObject.PodcastMap.Podcasts)
			{
				await pod.Value.CheckForNew();
				await pod.Value.FillNewEpisodes();
			}
			await PodcastFunctions.LoadLatestPodcastList();
			recentListData.List = PodcastFunctions.LatestPodcastList;
		}

		private void DownloadRecentClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button?.DataContext is KeyValuePair<string, PodcastEpisode>)
			{
				var kvp = ((KeyValuePair<string, PodcastEpisode>)button.DataContext);
				var podcast = config.ConfigObject.PodcastMap.Podcasts.FirstOrDefault(x => x.Value.PrimaryName == kvp.Key).Value;
				var ep = kvp.Value.EpisodeNumber;
				if (podcast == null)
					return;
				DownloadEpisode(ep, podcast).ConfigureAwait(false);
			}
			else
				return;
		}

		private async Task DownloadEpisode(int ep, PodcastDirectory podcast)
		{
			await podcast.DownloadEpisode(ep);
		}
	}

	public class RecentPodcastListData : INotifyPropertyChanged
	{
		Dictionary<string, PodcastEpisode> _list = null;
		public Dictionary<string, PodcastEpisode> List
		{
			get
			{
				return _list;
			}
			set
			{
				_list = value;
				NotifyPropertyChanged("List");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}
}
