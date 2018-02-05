using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PodcastHelper.Resources
{
	public partial class ItemsControlTemplates
	{
		public delegate void DownloadRecentDel(object sender, RoutedEventArgs e);
		public static event DownloadRecentDel OnDownloadRecentEvent;

		public delegate void SelectEpisodeDel(object sender, RoutedEventArgs e);
		public static event SelectEpisodeDel OnSelectEpisodeEvent;

		public delegate void PlayEpisodeDel(object sender, RoutedEventArgs e);
		public static event PlayEpisodeDel OnPlayEpisodeEvent;

		private void DownloadRecentClicked(object sender, RoutedEventArgs e)
		{
			OnDownloadRecentEvent?.Invoke(sender, e);
		}

		private void SelectEpisodeClicked(object sender, RoutedEventArgs e)
		{
			OnSelectEpisodeEvent?.Invoke(sender, e);
		}

		private void PlayRecentClicked(object sender, RoutedEventArgs e)
		{
			OnPlayEpisodeEvent?.Invoke(sender, e);
		}
	}
}
