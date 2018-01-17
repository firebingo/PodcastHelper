using PodcastHelper.Function;
using PodcastHelper.Models;
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

		public MainPage()
		{
			InitializeComponent();
			config = Config.Instance;

			intilizePodcasts();
			podcastListItems.ItemsSource = PodcastFunctions.LatestPodcastList;
		}

		public async Task intilizePodcasts()
		{
			foreach (var pod in config.ConfigObject.PodcastMap.Podcasts)
			{
				await pod.Value.CheckForNew();
				await pod.Value.FillNewEpisodes();
			}
		}
	}
}
