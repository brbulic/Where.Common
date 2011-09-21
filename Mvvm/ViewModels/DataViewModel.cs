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

	}
}