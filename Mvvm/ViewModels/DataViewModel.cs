namespace Where.Common.Mvvm
{
	/// <summary>
	/// ViewModel for a page that displays a single piece of content
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DataViewModel<T> : PageViewModel where T : class
	{
		private T _displayableContent;

		public T PageData
		{
			get { return _displayableContent; }
			set
			{
				_displayableContent = value;
				OnPropertyChanged("PageData");
			}
		}

		protected DataViewModel()
		{
			_tombstoneKey = string.Format("PageDataTombstone_{0}", GetType().Name);
		}


		private readonly string _tombstoneKey;

		public sealed override void SaveToTombstone(PageCommon page)
		{
			base.SaveToTombstone(page);

			if (PageData != null)
				page.SaveObjectToApplicationState(_tombstoneKey, PageData);

			SaveUserDataToTombstone(page);
		}

		protected virtual void SaveUserDataToTombstone(PageCommon page)
		{
			
		}


		public sealed override void LoadFromTombstone(PageCommon page)
		{
			base.LoadFromTombstone(page);

			var result = page.RestoreObjectFromApplicationState<T>(_tombstoneKey);

			if (result != null)
				PageData = result;

			LoadUserDataFromTombstone(page);

		}

		protected virtual void LoadUserDataFromTombstone(PageCommon page)
		{
			
		}
	}



}