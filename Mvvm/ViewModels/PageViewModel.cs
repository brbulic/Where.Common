using System.Diagnostics;

namespace Where.Common.Mvvm
{
	/// <summary>
	/// A standard view model that implements PageTitle and IsLoading
	/// </summary>
	public abstract class PageViewModel : ViewModelBase
	{
		/// <summary>
		/// Bind and set this to your page title when needed
		/// </summary>
		public override sealed string PageTitle
		{
			get
			{
				return base.PageTitle;
			}
			protected set
			{
				base.PageTitle = value;
			}
		}

		private volatile bool _useChainloading;

		/// <summary>
		/// Use this property modify the behaviour of IsLoading. 
		/// When not using Chain Loading, setting IsLoading will immediately change the IsLoading Property. 
		/// If it's being used, IsLoading will use an internal count to chain loadings. 
		/// Set to true to increase the chain, and false to decrease the chain.
		/// </summary>
		public bool EnableChainloading
		{
			get { return _useChainloading; }
			set { _useChainloading = value; }
		}


		protected PageViewModel()
		{
			PageTitle = "some page title";
		}

		private int _threadPageCounter;

		private bool _isLoading;
		/// <summary>
		/// Set to true if you want to display a loading animation.
		/// </summary>
		public bool IsLoading
		{
			get { return _isLoading; }
			set
			{
				if (_useChainloading)
				{
					if (value)
						_threadPageCounter++;
					else
						_threadPageCounter--;

					if (_threadPageCounter < 0)
						_threadPageCounter = 0;

					_isLoading = _threadPageCounter > 0;
				}
				else
				{
					_isLoading = value;
				}

				OnPropertyChanged("IsLoading");
			}
		}

		/// <summary>
		/// A neccessary override of this method will be used to take data from the UserState object and load/start loading the data.
		/// </summary>
		/// <param name="userState">See and use PageCommon's TransferObject to forward state data to the PageViewModel</param>
		public abstract void AttachData(object userState);


		/// <summary>
		/// Overriding this method will call the code on Every OnNavigatedTo event after the page has been already loaded (OnNavigatedTo using backward navigation)
		/// </summary>
		/// <param name="userState">See and use PageCommon's TransferObject to forward state data to the PageViewModel</param>
		public virtual void UpdateData(object userState)
		{
			Debug.WriteLine("==> ViewModelOperation: UpdatingData OnNavigatedTo...");
		}

		/// <summary>
		/// Override this method to control loading operations from tombstone. 
		/// IMPORTANT: if this method is automatically called then AttachData method from the base class is not invoked automatically.
		/// </summary>
		/// <param name="page"></param>
		public override void LoadFromTombstone(PageCommon page)
		{
			Debug.WriteLine("==> ViewModel Operations: Loading page {0} from tombstone...", page.GetType().Name);
		}


		/// <summary>
		/// Override this method to control saving operations to tombstone.
		/// </summary>
		/// <param name="page"></param>
		public override void SaveToTombstone(PageCommon page)
		{
			Debug.WriteLine("==> ViewModel Operations: Saving page {0} to tombstone...", page.GetType().Name);
		}

	}
}
