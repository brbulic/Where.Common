using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace Where.Common
{
	public sealed class BindableFavoritesCollection<T> : ObservableCollection<T>, IEquatable<BindableFavoritesCollection<T>>
	{

		private readonly Func<T, T, bool> _checkEquals;

		public IComparer<T> LastUsedComparer { get; private set; }

		private readonly List<T> _backingList;

		public bool IsSorted { get; private set; }

		private bool _recalculateHashesAndEquals = true;


		/// <summary>
		///  Uses default checkequals
		/// </summary>
		public BindableFavoritesCollection()
		{
			_backingList = (List<T>)Items;
		}

		/// <summary>
		/// Create a new collection. You could implement the IEquatable or use a optional Func to check if elements are equal
		/// </summary>
		/// <param name="checkEquals">Func to check equality</param>
		public BindableFavoritesCollection(Func<T, T, bool> checkEquals = null)
			: this()
		{
			_checkEquals = checkEquals;
		}


		/// <summary>
		/// Create a new collection from existing IEnumerable. You could implement the IEqutable or use a optional Func to check if elements are equal.
		/// </summary>
		/// <param name="existingEnumerable">Existing collection</param>
		/// <param name="checkEquals">Func to check equality</param>
		public BindableFavoritesCollection(IEnumerable<T> existingEnumerable, Func<T, T, bool> checkEquals = null)
			: this(checkEquals)
		{
			foreach (var items in existingEnumerable)
				AddToFavorites(items);
		}


		/// <summary>
		/// Add to favorites, returns true if success!
		/// </summary>
		/// <param name="item">Addable item</param>
		/// <param name="comparable">if you want to resort a collection after adding, pass it an IComparer. Otherwise, it uses the last used comparer</param>
		/// <returns>true if success, false if contains</returns>
		public bool AddToFavorites(T item, IComparer<T> comparable = null)
		{
			var contains = false;

			if (Contains(item))
				contains = true;
			else if (_checkEquals != null)
			{
				if (_backingList.Any(item1 => _checkEquals(item1, item)))
					contains = true;
			}

			if (contains)
				return false;

			Add(item);
			if (comparable != null)
				SortCollection(comparable);
			else if (LastUsedComparer != null)
				SortCollection(LastUsedComparer);
			else
			{
				IsSorted = false;
			}

			return true;
		}

		/// <summary>
		/// Remove a <b>item</b> from BindableFavoritesCollection. Returns <b>true</b> if successful!
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool RemoveFromFavorites(T item)
		{
			var contains = true;

			if (!Contains(item))
				contains = false;
			else if (_checkEquals != null)
			{
				if (!_backingList.Any(containedItem => _checkEquals(containedItem, item)))
					contains = false;
			}

			if (!contains)
				return false;


			Remove(item);
			return true;
		}

		/// <summary>
		/// Sort collection by comparer. You must provide your own comparer!!!
		/// </summary>
		/// <param name="comparer">Own comparer</param>
		public void SortCollection(IComparer<T> comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");

			LastUsedComparer = comparer;
			_backingList.Sort(comparer);
			_recalculateHashesAndEquals = true;
			Deployment.Current.Dispatcher.BeginInvoke(() => OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
			IsSorted = true;
			
		}

		public IEnumerable<T> ReturnInnerCollection()
		{
			return Items;
		}


		/// <summary>
		/// Reloads the entire collection from the updateable collection
		/// </summary>
		/// <param name="updateables">Reloadable arguments</param>
		/// <param name="comparer">comparer to use with sorting</param>
		public void UpdateCollection(IEnumerable<T> updateables, IComparer<T> comparer = null)
		{
			Items.Clear();

			foreach (var updateable in updateables)
				Items.Add(updateable);

			if (comparer != null)
				SortCollection(comparer);
			else if (LastUsedComparer != null)
				SortCollection(LastUsedComparer);
			else
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}


		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			_recalculateHashesAndEquals = true;
			base.OnCollectionChanged(e);
		}

		#region Implementation of IEquatable<BindableFavoritesCollection<T>>


		private int _hashCodeCache;
		
		public override int GetHashCode()
		{
			if (_recalculateHashesAndEquals)
			{
				var itemHashes = Items.Aggregate(string.Empty, (current, itemHash) => String.Format("{0}{1}", current, itemHash));
				var hashCodeString = string.Format("{0}{1}", Count, itemHashes);
				_hashCodeCache = hashCodeString.GetHashCode();
				_recalculateHashesAndEquals = false;
			}
			return _hashCodeCache;
		}

		public bool Equals(BindableFavoritesCollection<T> other)
		{
			return GetHashCode().Equals(other.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			var one = GetHashCode();
			var two = obj.GetHashCode();
			return one.Equals(two);
		}

		#endregion
	}
}
