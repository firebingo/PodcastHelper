using PodcastHelper.Function;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace PodcastHelper.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public delegate void onWindowChanged(double width, double height);
		public static event onWindowChanged OnMainWindowSizeChanged;
		private readonly List<Control> _pages;
		private VisiblePage _visiblePage;
		public VisiblePage VisiblePage
		{
			get { return _visiblePage; }
			set
			{
				_visiblePage = value;
				ChangePage();
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			_visiblePage = VisiblePage.Control;
			_pages = new List<Control>
			{
				mainPage,
				momentsPage
			};
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			WindowPlacementHandler.SetPlacement(new WindowInteropHelper(this).Handle);
			OnMainWindowSizeChanged?.Invoke(this.Width, this.Height);

			mainPage.Visibility = Visibility.Visible;
		}

		private void ChangePage()
		{
			foreach (var p in _pages)
			{
				p.Visibility = Visibility.Hidden;
			}
			switch (_visiblePage)
			{
				case VisiblePage.Control:
					mainPage.Visibility = Visibility.Visible;
					break;
				case VisiblePage.Moments:
					momentsPage.Visibility = Visibility.Visible;
					break;
			}
		}

		private void WindowClosing(object sender, CancelEventArgs e)
		{
			FileDownloader.Kill();
			VlcApi.Kill();
			PodcastFunctions.Kill();
			WindowPlacementHandler.GetPlacement(new WindowInteropHelper(this).Handle);
		}

		private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
		{
			OnMainWindowSizeChanged?.Invoke(this.Width, this.Height);
		}

		private void TopBarMouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void MinimizeClicked(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void CloseClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void WindowActivated(object sender, EventArgs e)
		{
			topGridBorder.BorderBrush = Application.Current.Resources["TopBarBorderActivated"] as SolidColorBrush;
		}

		private void WindowDeactivated(object sender, EventArgs e)
		{
			topGridBorder.BorderBrush = Application.Current.Resources["TopBarBorderDeactivated"] as SolidColorBrush;
		}
	}

	public enum VisiblePage
	{
		Control = 0,
		Moments = 1
	}
}
