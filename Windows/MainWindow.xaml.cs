using PodcastHelper.Function;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PodcastHelper.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public delegate void onWindowChanged(double width, double height);
		public static event onWindowChanged OnMainWindowSizeChanged;
		private VisiblePage visiblePage;

		public MainWindow()
		{
			InitializeComponent();

			visiblePage = VisiblePage.Control;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			WindowPlacementHandler.SetPlacement(new WindowInteropHelper(this).Handle);
			OnMainWindowSizeChanged?.Invoke(this.Width, this.Height);

			mainPage.Visibility = Visibility.Visible;
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
	}

	public enum VisiblePage
	{
		Control = 0
	}
}
