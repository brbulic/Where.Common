using System;

namespace Where.Common.Mvvm
{
	public abstract class CallbackViewModel : PageViewModel
	{
		private Action<bool, string> _statusReportCallback;
		public Action<bool, string> StatusReportCallback
		{
			get { return _statusReportCallback; }
			protected set
			{
				_statusReportCallback = value;
				OnPropertyChanged("StatusReportCallback");
			}
		}

		protected CallbackViewModel(Action<bool, string> callback)
		{
			StatusReportCallback = callback;
		}
	}
}
