using PodcastHelper.Function;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PodcastHelper.Models
{
	public class ErrorData : INotifyPropertyChanged
	{
		private string _error;
		public string Error
		{
			get
			{
				return _error;
			}
			set
			{
				_error = value;
				NotifyPropertyChanged("Error");
			}
		}

		public ErrorData()
		{
			ErrorTracker.UpdateError += OnUpdateError;
		}

		private void OnUpdateError(string error)
		{
			Error = error;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}

	public class RecentPodcastListData : INotifyPropertyChanged
	{
		private object ListLock = new object();

		List<PodcastEpisodeView> _list = null;
		public List<PodcastEpisodeView> RecentList
		{
			get
			{
				return _list;
			}
			set
			{
				lock (ListLock)
				{
					_list.Clear();
					_list = value;
				}
				NotifyPropertyChanged("RecentList");
			}
		}

		public RecentPodcastListData()
		{
			_list = new List<PodcastEpisodeView>();
		}

		public void UpdateRecentList(Dictionary<string, PodcastEpisode> list)
		{
			try
			{
				lock (ListLock)
				{
					_list = new List<PodcastEpisodeView>();
					foreach (var item in list)
					{
						_list.Add(new PodcastEpisodeView(item));
					}
				}
				NotifyPropertyChanged("RecentList");
			}
			catch(Exception ex)
			{
				ErrorTracker.CurrentError = ex.Message;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}

	public class PodcastEpisodeView
	{
		public string PrimaryName { get; set; }
		public bool NotIsDownloaded
		{
			get
			{
				return !Episode.IsDownloaded;
			}
		}
		public PodcastEpisode Episode { get; set; }

		public PodcastEpisodeView(KeyValuePair<string, PodcastEpisode> key)
		{
			PrimaryName = key.Key;
			Episode = key.Value;
		}

		public PodcastEpisodeView(string name, PodcastEpisode ep)
		{
			PrimaryName = name;
			Episode = ep;
		}
	}

	public class SearchPodcastData : INotifyPropertyChanged
	{
		private string _searchString;
		public string SearchString
		{
			get
			{
				return _searchString;
			}
			set
			{
				_searchString = value;
				NotifyPropertyChanged("SearchString");
			}
		}

		private List<PodcastEpisodeView> _results = null;
		public List<PodcastEpisodeView> SearchResults
		{
			get
			{
				return _results;
			}
			set
			{
				_results = value;
				NotifyPropertyChanged("SearchResults");
			}
		}

		private PodcastEpisodeView _currentEpisode = null;
		public PodcastEpisodeView CurrentEpisode
		{
			get
			{
				return _currentEpisode;
			}
			set
			{
				_currentEpisode = value;
				NotifyPropertyChanged("CurrentEpisode");
				NotifyPropertyChanged("ShowCurrent");
				NotifyPropertyChanged("MaxHeight");
			}
		}

		public Visibility ShowCurrent
		{
			get
			{
				return _currentEpisode == null ? Visibility.Hidden : Visibility.Visible;
			}
		}

		public int MaxHeight
		{
			get
			{
				return ShowCurrent == Visibility.Visible ? 125 : 300;
			}
		}

		public SearchPodcastData()
		{
			_searchString = string.Empty;
			_results = new List<PodcastEpisodeView>();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}
}
