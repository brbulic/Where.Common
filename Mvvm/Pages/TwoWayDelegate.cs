using System;
using System.Windows;
using System.Windows.Data;

namespace Where.Common.Mvvm
{
	public class DelegatePageCommon : PageCommon
	{

		public Action<bool, string> PageCallback
		{
			get { return (Action<bool, string>)GetValue(PageCallbackProperty); }
			set { SetValue(PageCallbackProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PageCallback.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PageCallbackProperty =
			DependencyProperty.Register("PageCallback", typeof(Action<bool, string>), typeof(DelegatePageCommon), new PropertyMetadata(null));

		public new CallbackViewModel PageViewModel
		{
			get { return (CallbackViewModel)base.PageViewModel; }
		}

		private void InitBinding()
		{
			var binding = new Binding("StatusReportCallback") { Source = PageViewModel, Mode = BindingMode.TwoWay };
			SetBinding(PageCallbackProperty, binding);
		}

		protected DelegatePageCommon()
		{
			InitBinding();
		}

		protected DelegatePageCommon(CallbackViewModel viewModel)
			: base(viewModel)
		{
			InitBinding();
		}
	}
}
