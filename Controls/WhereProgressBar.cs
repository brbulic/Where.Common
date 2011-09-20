using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Where.Common.Controls.PerformanceBarBase;

namespace Where.Common.Controls
{
	public sealed class WhereProgressBar : PerformanceProgressBar
	{

		public WhereProgressBar()
		{
			Visibility = Visibility.Collapsed;
			IsIndeterminate = false;
			Foreground = new SolidColorBrush(Colors.White);
		}

		public bool ShowLoading
		{
			get { return (bool)GetValue(ShowLoadingProperty); }
			set { SetValue(ShowLoadingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowLoading.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowLoadingProperty =
			DependencyProperty.Register("ShowLoading", typeof(bool), typeof(WhereProgressBar), new PropertyMetadata(false, OnLoadingChanged));

		private static void OnLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var self = (WhereProgressBar)d;

			var show = (bool)e.NewValue;

			if (show)
			{
				self.Visibility = Visibility.Visible;
				self.IsIndeterminate = true;
			}
			else
			{
				self.Visibility = Visibility.Collapsed;
				self.IsIndeterminate = false;
			}
		}
	}
}
