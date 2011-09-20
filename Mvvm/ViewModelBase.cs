using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Where.Common.Mvvm
{
	/// <summary>
	/// Base class for all ViewModel operations. 
	/// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
		private string _pageTitleBackingField;
        public virtual string PageTitle
        {
            get
            {
                return _pageTitleBackingField;
            }
            protected set
            {
                _pageTitleBackingField = value;
                OnPropertyChanged("PageTitle");
            }
        }

        public abstract void LoadFromTombstone(PageCommon page);

        public abstract void SaveToTombstone(PageCommon page);
        
		/// <summary>
		/// Cleans up all attached event handlers to the Page. If overridden, call the base method to invoke event cleanup.
		/// </summary>
        public virtual void Cleanup()
        {
            foreach (var propertyChangedEventHandler in _handlers)
            {
                var dumb = propertyChangedEventHandler;
                InternalPropertyChangedEvent -= dumb;
            }

            _handlers.Clear();
        }

        ~ViewModelBase()
        {
            Debug.WriteLine("ViewModel {0}, Hash: {1} GC'd on {2:f}", GetType().Name, GetHashCode(), DateTime.Now);
        }

        #region Implementation of INotifyPropertyChanged

        private event PropertyChangedEventHandler InternalPropertyChangedEvent;
        
        private readonly IList<PropertyChangedEventHandler> _handlers = new List<PropertyChangedEventHandler>();
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _handlers.Add(value);
                InternalPropertyChangedEvent += value;
            }
            remove
            {
                _handlers.Remove(value);
                InternalPropertyChangedEvent -= value;
            }
        }

        protected void OnPropertyChanged(String propertyName)
        {
            var propertyChangedDumb = InternalPropertyChangedEvent;

            if (propertyChangedDumb != null)
            {
                propertyChangedDumb(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
