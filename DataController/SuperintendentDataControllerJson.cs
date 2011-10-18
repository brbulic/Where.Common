using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Where.Common.DataController.Interfaces;
using Where.Common.Diagnostics;
using Where.Common.Services;
using Where.Common.Services.Interfaces;

namespace Where.Common.DataController
{
	public class SuperintendentDataControllerJson<T> : SuperintendentDataControllerBasic<T> where T: class, ISuperintendentDataContainer
	{

		#region File name caches privates

		private readonly string _currentControlledDataDirectory = DataControllerConfig.CreateDataDirectoryFromControlledData(typeof(T));

		private readonly IDictionary<string, string> _fileNameCache = new Dictionary<string, string>();

		#endregion

		#region other stuff

		private readonly HandleLocker<IDictionary<string, PerstistenceState>> _persistenceStatus;

		private readonly IThreadObjectQueue<BackgroundWorkerData> _writerQueuer;
		
		#endregion


		#region Scheduled writer

		private readonly Dictionary<String, object> _pendingWriting = new Dictionary<string, object>();

		private readonly DispatcherTimer _timer = new DispatcherTimer();

		#endregion

		public SuperintendentDataControllerJson(T instance)
			: base(instance)
		{
			_writerQueuer = new NewThreadObjectQueue<BackgroundWorkerData>();

			_persistenceStatus = new HandleLocker<IDictionary<string, PerstistenceState>>(new Dictionary<string, PerstistenceState>(), new object());

			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				_timer.Tick += Executor;
				_timer.Interval = TimeSpan.FromSeconds(15);
				_timer.Start();

				Application.Current.Exit += SuperintendentSignsOff;
			});
		}

		void SuperintendentSignsOff(object sender, EventArgs e)
		{
			_timer.Stop();
			_onExit = true;
			Executor(null, null); // Write all pending objects
		}


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

		private bool JsonExists(string propertyName)
		{
			var filename = GetFileNameForProperty(propertyName);
			return IsolatedStorageBase.FileExists(filename);
		}

		protected override SuperindententDataObject<TValue> GetValueFromUserStorage<TValue>(string propertyName, TValue defaultValue)
		{
			if (!JsonExists(propertyName))
			{
				return new SuperindententDataObject<TValue>(SuperintendentStatus.StatusOk, defaultValue, propertyName);
			}

			return GrabFromJson<TValue>(propertyName);
		}

		protected override void SendValueForWritingInUserStorage(string propertyName, object dataValues)
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

		#region getter operations

		protected SuperindententDataObject<TE> GrabFromJson<TE>(string propertyName)
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

		#region Writer queues and serializables

		private volatile bool _onExit;

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

		#endregion

		#region Private helpers

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
