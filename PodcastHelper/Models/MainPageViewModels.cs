using PodcastHelper.Function;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

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
		private readonly object _listLock = new object();

		List<PodcastEpisodeView> _list = null;
		public List<PodcastEpisodeView> RecentList
		{
			get
			{
				return _list;
			}
			set
			{
				lock (_listLock)
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
				lock (_listLock)
				{
					_list = new List<PodcastEpisodeView>();
					foreach (var item in list)
					{
						_list.Add(new PodcastEpisodeView(item));
					}
				}
				NotifyPropertyChanged("RecentList");
			}
			catch (Exception ex)
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

	public class RecentPlayedListData : INotifyPropertyChanged
	{
		private readonly object _listLock = new object();

		List<PodcastEpisodeView> _list = null;
		public List<PodcastEpisodeView> RecentList
		{
			get
			{
				return _list;
			}
			set
			{
				lock (_listLock)
				{
					_list.Clear();
					_list = value;
				}
				NotifyPropertyChanged("RecentList");
			}
		}

		public RecentPlayedListData()
		{
			_list = new List<PodcastEpisodeView>();
		}

		public void UpdateRecentList(Dictionary<string, PodcastEpisode> list)
		{
			try
			{
				lock (_listLock)
				{
					_list = new List<PodcastEpisodeView>();
					foreach (var item in list)
					{
						_list.Add(new PodcastEpisodeView(item));
					}
				}
				NotifyPropertyChanged("RecentList");
			}
			catch (Exception ex)
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
				return ShowCurrent == Visibility.Visible ? 115 : 260;
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

	public class TimeSliderData : INotifyPropertyChanged
	{
		private int _width;
		public int Width
		{
			get
			{
				return _width;
			}
			set
			{
				_width = value;
				NotifyPropertyChanged("Width");
			}
		}

		//Note that this should be between 0-100
		private double _sliderPos;
		public double SliderPosition
		{
			get
			{
				return _sliderPos;
			}
			set
			{
				_sliderPos = value;
				NotifyPropertyChanged("SliderPosition");
				NotifyPropertyChanged("CurrentValue");
			}
		}

		public bool MovingTimeSlider { get; set; }

		private TimeSpan _maxValue;
		public TimeSpan MaxValue
		{
			get
			{
				return _maxValue;
			}
			set
			{
				_maxValue = value;
				NotifyPropertyChanged("MaxValue");
				NotifyPropertyChanged("CurrentValue");
			}
		}

		public TimeSpan CurrentValue
		{
			get
			{
				return new TimeSpan(0, 0, (int)(_maxValue.TotalSeconds * (_sliderPos / 100)));
			}
		}

		public TimeSliderData()
		{
			_width = 0;
			_sliderPos = 0.0;
			_maxValue = new TimeSpan(0, 0, 0);
			PodcastFunctions.PlayingEpisodeChangedEvent += PlayingEpisodeChanged;
		}

		private void PlayingEpisodeChanged(PodcastEpisode episode)
		{
			if (episode != null)
			{
				if (!MovingTimeSlider)
					SliderPosition = episode.Progress.Progress * 100;
				if (_maxValue.Ticks != episode.Progress.Length.Ticks)
					MaxValue = episode.Progress.Length;
			}
			else
			{
				SliderPosition = 0.0;
				MaxValue = new TimeSpan(0, 0, 0);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}

	public class OtherMainPageData : INotifyPropertyChanged
	{
		private bool _downloadingFile;
		public bool DownloadingFile
		{
			get
			{
				return _downloadingFile;
			}
			set
			{
				_downloadingFile = value;
				NotifyPropertyChanged("DownloadingFile");
			}
		}

		private float _downloadProgress;
		public float DownloadProgress
		{
			get
			{
				return _downloadProgress;
			}
			set
			{
				_downloadProgress = value;
				_downloadingFile = true;
				NotifyPropertyChanged("DownloadProgress");
				NotifyPropertyChanged("DownloadingFile");
			}
		}

		private ImageSource _albumArt;
		public ImageSource AlbumArt
		{
			get
			{
				return _albumArt;
			}
			set
			{
				_albumArt = value;
				NotifyPropertyChanged("AlbumArt");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}
}
