using PodcastHelper.Function;
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
		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			WindowPlacementHandler.SetPlacement(new WindowInteropHelper(this).Handle);
		}

		private void WindowClosing(object sender, CancelEventArgs e)
		{
			FileDownloader.Kill();
			VlcApi.Kill();
			WindowPlacementHandler.GetPlacement(new WindowInteropHelper(this).Handle);
		}
	}
}
