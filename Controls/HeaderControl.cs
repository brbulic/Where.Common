using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Where.Common.Controls
{
	public class HeaderControl : ContentControl
	{
		/// <summary>
		/// Sets the page title
		/// </summary>
		public string PageTitle
		{
			get { return (string)GetValue(PageTitleProperty); }
			set { SetValue(PageTitleProperty, value); }
		}

		public static readonly DependencyProperty PageTitleProperty =
			DependencyProperty.Register("PageTitle", typeof(string), typeof(HeaderControl), new PropertyMetadata(string.Empty));


		public object FooterContent
		{
			get { return (object)GetValue(FooterContentProperty); }
			set { SetValue(FooterContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FooterContent.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FooterContentProperty =
			DependencyProperty.Register("FooterContent", typeof(object), typeof(HeaderControl), new PropertyMetadata(null));



		public bool ShowLoading
		{
			get { return (bool)GetValue(ShowLoadingProperty); }
			set { SetValue(ShowLoadingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowLoading.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowLoadingProperty =
			DependencyProperty.Register("ShowLoading", typeof(bool), typeof(HeaderControl), new PropertyMetadata(false));

		public HeaderControl()
		{
			DefaultStyleKey = typeof(HeaderControl);

		}


		~HeaderControl()
		{
			Debug.WriteLine("=====> GC: {0} cleaned from memory. Hash: {1}", GetType().Name, GetHashCode());
		}
	}
}
