using System;
using System.Collections;
using System.Collections.Generic;

namespace Where.Common
{
	/// <summary>
	/// Creates a Read-only Enumerable ICollection that uses List as backing. Useful for creating local copies for iteration./>
	/// </summary>
	/// <typeparam name="T">Some type you use</typeparam>
	public class ReadOnlyEnumerable<T> : ICollection<T>
	{
		private readonly List<T> _internalList = new List<T>();

		/// <summary>
		/// Creates a new Read-Only collection from an IEnumerable.
		/// </summary>
		/// <param name="source"></param>
		public ReadOnlyEnumerable(IEnumerable<T> source)
		{
			_internalList.AddRange(source);
		}

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<T> GetEnumerator()
		{
			return _internalList.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<T>

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// [NOT SUPPORTED]
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		/// <exception cref="NotSupportedException"/>
		public void Add(T item)
		{
			throw new NotSupportedException("This is a read only collection");
		}

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// [NOT SUPPORTED]
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
		public void Clear()
		{
			throw new NotSupportedException("This is a read only collection");
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(T item)
		{
			return _internalList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_internalList.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// [NOT SUPPORTED]
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		/// <exception cref="NotSupportedException" />
		public bool Remove(T item)
		{
			throw new NotSupportedException("This is a read only collection");
		}

		public int Count
		{
			get { return _internalList.Count; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		#endregion
	}
}
