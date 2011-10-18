using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Where.Common.DataController.Interfaces;
using Where.Common.Diagnostics;
using Where.Common.Services;
using Where.Common.Services.Interfaces;

namespace Where.Common.DataController
{
	/// <summary>
	/// Abstract implementation of the ISuperintendentDataCore interface without data writing.
	/// </summary>
	/// <typeparam name="T">Controlls the instance's properties</typeparam>
	public abstract class SuperintendentDataControllerBasic<T> : ISuperintendentDataCore<T> where T : class, ISuperintendentDataContainer
	{

		/// <summary>
		/// All operations with Memory cache should be made thread safe
		/// </summary>
		[SecurityCritical]
		private readonly HandleLocker<IDictionary<string, object>> _memoryCache;

		private readonly T _currentInstance;

		protected SuperintendentDataControllerBasic(T instance)
		{
			_memoryCache = new HandleLocker<IDictionary<string, object>>(new Dictionary<string, object>(), new object());
			_currentInstance = instance;
			instance.PropertyChanged += OnSomePropertyChanged;
		}

		private void OnSomePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var getProperty = SuperintendentBase<T>.GetPropertyType(e.PropertyName);

			var attrib = getProperty.GetCustomAttributes(false);

			foreach (var o in attrib)
			{
				if (o is SerializableSharedDataAttribute)
				{
					var attr = o as SerializableSharedDataAttribute;
					if (attr.IgnoreSerialization)
					{
						Debug.WriteLine("This should be ignored... {0}", e.PropertyName);
						return;
					}
				}
			}

			StoreCachedValue(e.PropertyName);
		}

		private static Type GetPropertyType(string propertyName)
		{
			return SuperintendentBase<T>.GetPropertyType(propertyName);
		}

		#region Private helpers

		private SuperindententDataObject<TValue> RetrieveFromLocalCache<TValue>(string propertyName, TValue defaultValue = default(TValue))
		{
			var value = _memoryCache.ExecuteSafeOperationOnObject(dictionary =>
			{
				if (dictionary.ContainsKey(propertyName))
				{
					var cachedValue = (TValue)dictionary[propertyName];
					return new SuperindententDataObject<TValue>(SuperintendentStatus.StatusOk, cachedValue, propertyName);
				}

				if (EqualityComparer<TValue>.Default.Equals(default(TValue), defaultValue))
				{	// defaultValue is empty
					return new SuperindententDataObject<TValue>(SuperintendentStatus.NeedsReload, defaultValue, propertyName);
				}

				dictionary.Add(propertyName, defaultValue);
				return new SuperindententDataObject<TValue>(SuperintendentStatus.Changed, defaultValue, propertyName);
			});

			return value;
		}

		#endregion

		#region Implementation of ISuperintendentDataCore<T>

		public T ControlledInstance
		{
			get { return _currentInstance; }
		}

		public void StoreCachedValue(string propertyName)
		{

			if (!_currentInstance.Initialized)
				return;

			var getValue = _memoryCache.ExecuteSafeOperationOnObject(myCachedData => myCachedData.ContainsKey(propertyName) ? myCachedData[propertyName] : null);

			if (getValue != null)
			{
				WhereDebug.WriteLine(string.Format("=====> SuperintendentDataCore: Started saving \"{0}\" from propertyChanged...", propertyName));
				SendValueForWritingInUserStorage(propertyName, getValue);
			}
			else
				WhereDebug.WriteLine(string.Format("Cannot find cached data for writing for property \"{0}\".", propertyName));
		}

		public SuperindententDataObject<TE> RetrieveValue<TE>(string propertyName, TE defaultValue = default(TE))
		{
			if (!_currentInstance.Initialized)
				return new SuperindententDataObject<TE>(SuperintendentStatus.Changed, defaultValue, propertyName);

			var result = RetrieveFromLocalCache(propertyName, defaultValue);

			switch (result.CurrentObjectStatus)
			{
				case SuperintendentStatus.StatusOk:
					return result;							// Returns here if cached version exists
				case SuperintendentStatus.Changed:
				case SuperintendentStatus.NeedsReload:
					break;									// Let's get it from some storage if not. Let's see if json has it...
				default:
					throw new ArgumentOutOfRangeException();
			}

			result = GetValueFromUserStorage(propertyName, defaultValue);

			if (EqualityComparer<TE>.Default.Equals(result.Value, defaultValue))
			{
				// Storage doesn't have it... using default then. Enqueue a write and continue with standard tasks.
				SendValueForWritingInUserStorage(propertyName, result.Value);
			}

			// If the object is good, save it to memory.
			if (result.CurrentObjectStatus == SuperintendentStatus.StatusOk)
			{
				Debug.WriteLine("=====> SuperintendentDataCore: JSON of property \"{0}\" exists, retrieving from IsolatedStorage (should happen only once).", propertyName);
				_memoryCache.ExecuteSafeOperationOnObject(dict =>
															{
																dict.SetValueInDictionary(propertyName, result.Value);
																return true;
															});
			}

			return result;
		}

		public void RetrieveValueAsync<TE>(string propertyName, TE defaultValue, Action<SuperindententDataObject<TE>, object> returns, object state)
		{
			ThreadPool.QueueUserWorkItem(myState =>
											{
												var dumb1 = propertyName;
												var dumb2 = defaultValue;
												var callback = returns;
												var dumb3 = state;
												var result = RetrieveValue(dumb1, dumb2);
												Deployment.Current.Dispatcher.BeginInvoke(() => callback(result, dumb3));
												GC.Collect();
											});
		}

		public void SaveValue<TE>(string propertyName, TE value)
		{
			SuperintendentBase<T>.ContainsPropertyOfType(typeof(TE), propertyName);

			/* real save operations */

			var getValue = RetrieveValue<TE>(propertyName).Value;
			if (EqualityComparer<TE>.Default.Equals(getValue, value))
			{
				Debug.WriteLine("Value is the same, skipping serialization!");
				GC.Collect();
				return;

			}

			_memoryCache.ExecuteSafeOperationOnObject(dict =>
			{
				dict.SetValueInDictionary(propertyName, value);
				return true;
			});

			SendValueForWritingInUserStorage(propertyName, value);

		}

		/// <summary>
		/// Accesses the user storage and retrieves the data item from storage.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="propertyName"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		protected abstract SuperindententDataObject<TValue> GetValueFromUserStorage<TValue>(string propertyName, TValue defaultValue = default(TValue));

		/// <summary>
		/// Sends the propertyname and value for user storage to write
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="dataValues"></param>
		protected abstract void SendValueForWritingInUserStorage(string propertyName, object dataValues);

		#endregion
	}
}
