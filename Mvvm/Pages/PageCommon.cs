using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace Where.Common.Mvvm
{
	/// <summary>
	/// Primary page for all page common operations
	/// </summary>
	public class PageCommon : PhoneApplicationPage
	{
		private readonly PageViewModel _myViewModel;

		private bool _isTombstone;

		public PageViewModel PageViewModel
		{
			get { return _myViewModel; }
		}

		protected object TransferObject;

		protected bool IsPageLoaded { get; set; }

		protected bool AttachDataCalled { get; private set; }

		public PageCommon()
		{
			LayoutUpdated += PageCommonLayoutUpdated;
		}

		public PageCommon(PageViewModel myViewModel)
			: this()
		{
			DataContext = _myViewModel = myViewModel;
		}


		private void PageCommonLayoutUpdated(object sender, EventArgs e)
		{
			LayoutUpdated -= PageCommonLayoutUpdated;
			PageLayoutUpdated(sender, e);
		}


		/// <summary>
		/// Override without calling the base method to override the default functionality.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void PageLayoutUpdated(object sender, EventArgs e)
		{
			if (PageViewModel != null)
			{
				if (!_isTombstone)
				{
					PageViewModel.AttachData(TransferObject);
					AttachDataCalled = true;
				}
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (PageViewModel != null)
			{
				if (AttachDataCalled)
					PageViewModel.UpdateData(TransferObject);

				switch (AppState.GetCurrentAppState)
				{
					case CurrentAppState.None:
						var includesPreviousState = this.ContainsStateElementsForPage();
						if (includesPreviousState)
						{
							if (_isTombstone)
								PageViewModel.LoadFromTombstone(this);
						}
						break;
					case CurrentAppState.Starting:
						Application.Current.SetCurrentAppState(CurrentAppState.None);
						break;
					case CurrentAppState.Resuming:
						Application.Current.SetCurrentAppState(CurrentAppState.None);
						break;
					case CurrentAppState.ResumingNotPreserved:
						_isTombstone = true;
						PageViewModel.LoadFromTombstone(this);
						Application.Current.SetCurrentAppState(CurrentAppState.None);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
				Application.Current.SetCurrentAppState(CurrentAppState.None);

			GC.Collect();
		}
		
		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			if (PageViewModel != null)
			{
				if (e.NavigationMode == NavigationMode.New)
					PageViewModel.SaveToTombstone(this);

				if (e.NavigationMode == NavigationMode.Back)
				{
					this.RemoveAllKeysFromPage();
					PageViewModel.Cleanup();
					TransferObject = null;
					DataContext = null;

				}
			}
		}

		~PageCommon()
		{
			Debug.WriteLine("=====> GC: Page {0} destroyed. Hash: {1}", GetType().Name, GetHashCode());
		}
	}
}
