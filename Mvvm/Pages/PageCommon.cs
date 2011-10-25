using System;
using System.Collections.Generic;
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

		private bool _pageActive;

		protected bool AttachDataCalled { get; private set; }

		private string _pageNameCache;
		public string PageName
		{
			get
			{
				if (string.IsNullOrEmpty(_pageNameCache))
				{
					_pageNameCache = string.Format("{0}_{1}", GetType().Name, Environment.TickCount);
				}

				return _pageNameCache;
			}
		}

		public PageCommon()
		{
			LayoutUpdated += PageCommonLayoutUpdated;
		}

		public PageCommon(PageViewModel myViewModel)
			: this()
		{
			_myViewModel = myViewModel;
			DataContext = _myViewModel;
		}


		private void PageCommonLayoutUpdated(object sender, EventArgs e)
		{
			LayoutUpdated -= PageCommonLayoutUpdated;
			PageLayoutUpdated(sender, e);

			if (PageViewModel != null)
			{
				if (!_isTombstone)
				{
					PageViewModel.AttachData(TransferObject);
					AttachDataCalled = true;
				}
			}


		}

		/// <summary>
		/// Override without calling the base method to override the default functionality.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void PageLayoutUpdated(object sender, EventArgs e)
		{

		}

		private readonly IDictionary<string, string> processQueriedString = new Dictionary<string, string>();

		/// <summary>
		/// Called when a page becomes the active page in a frame.
		/// </summary>
		/// <param name="e">An object that contains the event data.</param>
		[Obsolete("Deprecated. Use page PageOnNavigatedTo")]
		protected sealed override void OnNavigatedTo(NavigationEventArgs e)
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
						if (includesPreviousState && !_pageActive)
						{
							_isTombstone = true;
							RestoreFromTombstone();
							this.RemoveAllKeysFromPage();
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
						RestoreFromTombstone();
						Application.Current.SetCurrentAppState(CurrentAppState.None);
						this.RemoveAllKeysFromPage();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
				Application.Current.SetCurrentAppState(CurrentAppState.None);

			if (!_pageActive)
			{
				foreach (var mystr in NavigationContext.QueryString)
				{
					processQueriedString.Add(mystr);
				}
			}

			PageOnNavigatedTo(new PageCommonNavigationEventArgs(_pageActive, processQueriedString, _isTombstone, e.Content, e.Uri));

			_pageActive = true;
			GC.Collect();

		}

		protected virtual void PageOnNavigatedTo(PageCommonNavigationEventArgs args)
		{
			Debug.WriteLine("Calling on navigated to...!");
		}

		private NavigationMode _currentPageNavigationMode = NavigationMode.Refresh;

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			_currentPageNavigationMode = e.NavigationMode;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			if (PageViewModel != null)
			{
				if (_currentPageNavigationMode == NavigationMode.New)
					SaveToTombstone();

				if (_currentPageNavigationMode == NavigationMode.Back)
				{
					this.RemoveAllKeysFromPage();
					PageViewModel.Cleanup();
					TransferObject = null;
					DataContext = null;

				}
			}
		}


		private void SaveToTombstone()
		{
			State.SetValueInDictionary(TombstoneHelpers.PageNameTombstoneKey, PageName);

			if (PageViewModel != null)
				PageViewModel.SaveToTombstone(this);
		}

		private void RestoreFromTombstone()
		{
			var result = State.GetValueFromDictionary(TombstoneHelpers.PageNameTombstoneKey, PageName);

			if ((string)result != PageName && PageViewModel != null)
				PageViewModel.LoadFromTombstone(this);

		}

		~PageCommon()
		{
			Debug.WriteLine("=====> GC: Page {0} destroyed. Hash: {1}", GetType().Name, GetHashCode());
		}
	}
}
