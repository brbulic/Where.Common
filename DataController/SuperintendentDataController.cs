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
	/// Default implementation of the ISuperintendentDataCore interface.
	/// </summary>
	/// <typeparam name="T">Controlls the instance's properties</typeparam>
	public class SuperintendentDataController<T> : ISuperintendentDataCore<T> where T : class, ISuperintendentDataContainer
	{

		/// <summary>
		/// All operations with Memory cache should be made thread safe
		/// </summary>
		[SecurityCritical]
		private readonly HandleLocker<IDictionary<string, object>> _memoryCache;

		private readonly HandleLocker<IDictionary<string, PerstistenceState>> _persistenceStatus;

		private readonly IDictionary<string, string> _fileNameCache = new Dictionary<string, string>();

		private readonly string _currentControlledDataDirectory = DataControllerConfig.CreateDataDirectoryFromControlledData(typeof(T));

		private readonly IThreadObjectQueue<BackgroundWorkerData> _writerQueuer;

		private readonly T _currentInstance;


		#region Scheduled writer

		private readonly Dictionary<String, object> _pendingWriting = new Dictionary<string, object>();

		private readonly DispatcherTimer _timer = new DispatcherTimer();

		#endregion

		public SuperintendentDataController(T instance)
		{
			_persistenceStatus = new HandleLocker<IDictionary<string, PerstistenceState>>(new Dictionary<string, PerstistenceState>(), new object());
			_memoryCache = new HandleLocker<IDictionary<string, object>>(new Dictionary<string, object>(), new object());
			_writerQueuer = new NewThreadObjectQueue<BackgroundWorkerData>();

			Deployment.Current.Dispatcher.BeginInvoke(() =>
														{
															_timer.Tick += Executor;
															_timer.Interval = TimeSpan.FromSeconds(15);
															_timer.Start();

															Application.Current.Exit += SuperintendentSignsOff;
														});

			_currentInstance = instance;
			instance.PropertyChanged += OnSomePropertyChanged;
		}

		void SuperintendentSignsOff(object sender, EventArgs e)
		{
			_timer.Stop();
			_onExit = true;
			Executor(null, null); // Write all pending objects
		}

		private void OnSomePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			StoreCachedValue(e.PropertyName);
		}

		private static Type GetPropertyType(string propertyName)
		{
			return SuperintendendentBase<T>.GetPropertyType(propertyName);
		}

		#region Private helpers

		#region FileName Cache

		/// <summary>
		/// Get a file name by propertyName
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		private string GetFileNameForProperty(string propertyName)
		{
			string fileNameCache;

			if (_fileNameCache.ContainsKey(propertyName))
			{
				fileNameCache = _fileNameCache[propertyName];
			}
			else
			{
				fileNameCache = string.Format("{0}/{1}.json", _currentControlledDataDirectory, propertyName);
				Debug.WriteLine("=====> SuperintendentDataCore: Will start saving property \"{0}\" to file name \"{1}\"!", propertyName, fileNameCache);
				_fileNameCache.Add(propertyName, fileNameCache);
			}

			return fileNameCache;
		}

		#endregion

		private bool JsonSerializationAndPersistence(object state, string filename)
		{
			string jsonString = string.Empty;
			int retries = 0;

			while (retries < 3)
			{
				try
				{
					jsonString = JsonConvert.SerializeObject(state);
					break;
				}
				catch (Exception)
				{
					retries++;
					Debug.WriteLine("=====> SuperintendentDataCore: Save operation failed. Making a retry: ({0})", retries);
				}
			}

			if (!_onExit)
				IsolatedStorageStringWriter.GetWriter.BeginWriteStringToFile(jsonString, filename);
			else
				IsolatedStorageStringWriter.GetWriter.WriteStringToFileSync(jsonString, filename);


			return retries < 3 && !string.IsNullOrEmpty(filename);
		}


		#region getter operations

		private SuperindententDataObject<TE> GrabFromJson<TE>(string propertyName)
		{
			var fileNameString = GetFileNameForProperty(propertyName);
			var stringContents = IsolatedStorageStringWriter.GetWriter.ReadStringFromIsolatedStorage(fileNameString);


			if (stringContents == null)
				return new SuperindententDataObject<TE>(SuperintendentStatus.DataNotFound, default(TE), propertyName);


			SuperindententDataObject<TE> resultObject;

			try
			{
				var result = JsonConvert.DeserializeObject<TE>(stringContents);
				resultObject = new SuperindententDataObject<TE>(SuperintendentStatus.StatusOk, result, propertyName);
			}
			catch (Exception e)
			{
				resultObject = new SuperindententDataObject<TE>(SuperintendentStatus.DataNotFound, default(TE), propertyName);
				Utils.ErrorLogInstance.AddError("GrabFromJsonSDC", e.Message);

			}

			// Basically that's it
			return resultObject;
		}

		#endregion

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
				EnqueueWriteValue(propertyName, getValue);
			}

			else
				WhereDebug.WriteLine(string.Format("Cannot find cached data for writing for property \"{0}\".", propertyName));

		}

		private bool JsonExists(string propertyName)
		{
			var filename = GetFileNameForProperty(propertyName);
			return IsolatedStorageBase.FileExists(filename);
		}

		public SuperindententDataObject<TE> RetrieveValue<TE>(string propertyName, TE defaultValue = default(TE))
		{
			if (!_currentInstance.Initialized)
				return new SuperindententDataObject<TE>(SuperintendentStatus.Changed, defaultValue, propertyName);

			var result = RetrieveFromLocalCache(propertyName, defaultValue);

			switch (result.CurrentObjectStatus)
			{
				case SuperintendentStatus.StatusOk:
					return result;
				case SuperintendentStatus.Changed:
				case SuperintendentStatus.NeedsReload:
					break; // Let's see if json has it...
				default:
					throw new ArgumentOutOfRangeException();
			}


			if (!JsonExists(propertyName))
			{
				EnqueueWriteValue(propertyName, result.Value); // JSON doesn't exist, but default value does. Saves new value to json
				return new SuperindententDataObject<TE>(SuperintendentStatus.StatusOk, result.Value, propertyName);
			}

			// Retrieve value from JSON
			result = GrabFromJson<TE>(propertyName);
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

		#endregion

		#region Writer queues and serializables

		private volatile bool _onExit;

		private void EnqueueWriteValue(string propertyName, object dataValues)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
														{
															if (_pendingWriting.ContainsKey(propertyName))
																_pendingWriting[propertyName] = dataValues;
															else
																_pendingWriting.Add(propertyName, dataValues);
														});

			_persistenceStatus.ExecuteSafeOperationOnObject(value =>
																{
																	var before = value.GetValueFromDictionary(propertyName);
																	if (before == PerstistenceState.Saved || before == PerstistenceState.Unknown)
																		value.SetValueInDictionary(propertyName, PerstistenceState.PendingWrite);
																	return true;
																});
		}

		private void Executor(object sender, EventArgs eventArgs)
		{
			foreach (var kvp in _pendingWriting)
			{
				if (!_onExit)
				{
					var data = new BackgroundWorkerData(kvp.Key, kvp.Value);
					Predicate<BackgroundWorkerData> runner = val => val.PropertyName == data.PropertyName;

					if (_writerQueuer.OperationPending(runner))
						_writerQueuer.CancelOperation(runner);

					if (_writerQueuer.OperationRunning(runner))
						return;

					_writerQueuer.EnqueueOperation(WriterActionExecuter, OnResult, data);
				}
				else
				{
					var jsonString = JsonConvert.SerializeObject(kvp.Value);
					IsolatedStorageStringWriter.GetWriter.WriteStringToFileSync(jsonString, GetFileNameForProperty(kvp.Key));
				}
			}

			_pendingWriting.Clear();

		}

		private void OnResult(Guid arg1, QueueResult<PerstistenceState> arg2)
		{
			_persistenceStatus.ExecuteSafeOperationOnObject(dict =>
																{
																	var propertyName = (String)arg2.Request.TransferObject;
																	dict.SetValueInDictionary(propertyName, arg2.Result);
																	Debug.WriteLine("Save operation for \"{0}\" done right? Answer: {1}", propertyName, arg2.Result);
																	return true;
																});
		}

		public void SaveValue<TE>(string propertyName, TE value)
		{
			SuperintendendentBase<T>.ContainsPropertyOfType(typeof(TE), propertyName);

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

			EnqueueWriteValue(propertyName, value);

		}

		private PerstistenceState WriterActionExecuter(BackgroundWorkerData obj)
		{
			var data = obj;
			var propertyName = data.PropertyName;
			var value = data.Value;

			var res = _persistenceStatus.ExecuteSafeOperationOnObject(
				dict =>
				{

					var persistenceStatus = dict.GetValueFromDictionary(data.PropertyName);
					bool result;

					if (persistenceStatus == PerstistenceState.PendingWrite)
					{
						dict.SetValueInDictionary(propertyName, PerstistenceState.BeingWritten);

						var getFileName = GetFileNameForProperty(propertyName);
						WhereDebug.WriteLine(string.Format("Saving property \"{0}\" to filename \"{1}\"", propertyName, getFileName));
						result = JsonSerializationAndPersistence(value, getFileName);
					}
					else
					{
						throw new InvalidOperationException("How did i get here?");
					}


					var dsResult = result ? PerstistenceState.Saved : PerstistenceState.Empty;
					return dsResult;
				});

			return res;
		}


		private sealed class BackgroundWorkerData : IQueueOperationData
		{
			private readonly string _propertyName;
			private readonly object _value;

			public BackgroundWorkerData(string propertyName, object value)
			{
				_propertyName = propertyName;
				_value = value;
			}

			public object Value
			{
				get { return _value; }
			}

			public string PropertyName
			{
				get { return _propertyName; }
			}

			public override int GetHashCode()
			{
				return PropertyName.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				return GetHashCode().Equals(obj.GetHashCode());
			}

			#region Implementation of IQueueOperationData

			public object TransferObject
			{
				get { return _propertyName; }
			}

			#endregion
		}



		#endregion
	}
}
