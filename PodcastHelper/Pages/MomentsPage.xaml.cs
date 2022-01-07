using PodcastHelper.Windows;
using System.Windows;
using System.Windows.Controls;

namespace PodcastHelper.Pages
{
	/// <summary>
	/// Interaction logic for MomentsPage.xaml
	/// </summary>
	public partial class MomentsPage : Page
	{
		public MomentsPage()
		{
			InitializeComponent();
		}

		private void ControlsClicked(object sender, RoutedEventArgs e)
		{
			if (Window.GetWindow(this) is MainWindow w)
				w.VisiblePage = VisiblePage.Control;
		}
	}
}
