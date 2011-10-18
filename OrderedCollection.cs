using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;

namespace Where.Common
{
    public class OrderedCollection<T> : ICollection<T>, INotifyCollectionChanged
    {
        private const int MaxElements = 10;

        private readonly int _currentMaxElements;

        private readonly ObservableCollection<DataContainer> _backgingList = new ObservableCollection<DataContainer>();

        private ReadOnlyEnumerable<T> _myTempEnumerable;

        public OrderedCollection()
        {
            _backgingList.CollectionChanged += BackgingListCollectionChanged;
            _currentMaxElements = MaxElements;
        }

        public OrderedCollection(int maxElements)
        {
            _backgingList.CollectionChanged += BackgingListCollectionChanged;
            _currentMaxElements = maxElements;
        }

        void BackgingListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _myTempEnumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            var itemLocal = item;

            var contains = _backgingList.FirstOrDefault(element => element.Data.Equals(itemLocal));

            if (EqualityComparer<DataContainer>.Default.Equals(contains, default(DataContainer)))
            {
                if (_backgingList.Count == _currentMaxElements)
                {
                    //Remove(_backgingList[_backgingList.Count - 1].Data); // 5+ out of 5 :)

                    var getLastElement = _myTempEnumerable.Last();
                    Remove(getLastElement);
                }

                _backgingList.Add(new DataContainer(1, itemLocal));
            }
            else
            {
                contains.IncreaseCount();
            }

            var orderedCollection = _backgingList.OrderByDescending(elem => elem.Counter, Comparer<int>.Default).Select(elem => elem.Data);
            _myTempEnumerable = new ReadOnlyEnumerable<T>(orderedCollection);

        }

        public void Clear()
        {
            _backgingList.Clear();
            _myTempEnumerable = new ReadOnlyEnumerable<T>(_backgingList.Select(item => item.Data));
        }

        public bool Contains(T item)
        {
            return _backgingList.Any(elem => elem.Data.Equals(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            var elem = _backgingList.FirstOrDefault(element => element.Data.Equals(item));

            bool result;

            if (!EqualityComparer<DataContainer>.Default.Equals(elem, default(DataContainer)))
            {
                _backgingList.Remove(elem);
                _myTempEnumerable = new ReadOnlyEnumerable<T>(_backgingList.Select(element => element.Data));
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        public int Count
        {
            get { return _backgingList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ObservableCollection<DataContainer> GetBackgingCollection_CHAMPION()
        {
            return _backgingList;
        }
        
        public class DataContainer
        {
            private int _counter;
            private readonly T _data;

            public DataContainer(int counter, T data)
            {
                _counter = counter;
                _data = data;
            }

            public T Data
            {
                get { return _data; }
            }

            public static DataContainer CreateDefault()
            {
                return new DataContainer(1, default(T));
            }

            public void IncreaseCount()
            {
                _counter++;
            }

            public int Counter
            {
                get { return _counter; }
                set { _counter = value; }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
