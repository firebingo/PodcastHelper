﻿using PodcastHelper.Windows;
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
