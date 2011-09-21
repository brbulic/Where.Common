using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Where.Common.Mvvm
{
	/// <summary>
	/// Enchances the ViewModel to support BindableCollection
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ICollectionViewModel<T>
	{
		ObservableCollection<T> BindableCollection { get; }
	}

}
