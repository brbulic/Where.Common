using System.Windows;

namespace Where.Common.Mvvm
{
	/// <summary>
	/// View model for a page with a displayable message support
	/// </summary>
	public abstract class MessagePageViewModel : PageViewModel
	{

		private string _statusMessage;
		/// <summary>
		/// Bind this to a TextBlock in the View to display the message
		/// </summary>
		public string StatusMessage
		{
			get { return _statusMessage; }
			set
			{
				_statusMessage = value;
				DataVisible = string.IsNullOrEmpty(value) ? true : false;
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					OnPropertyChanged("StatusMessage");
					OnPropertyChanged("DataVisible");
				});
			}
		}

		/// <summary>
		/// Bind this to a VisibilityConverter or a DependencyProperty to modify the view for displaying message or data
		/// </summary>
		public bool DataVisible { get; private set; }

		protected MessagePageViewModel()
		{
			StatusMessage = null;
		}


	}
}
