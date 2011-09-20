using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Where.Common.DataController.Interfaces;
using Where.Common.DataController;

namespace Where.Common.DataController
{
	/// <summary>
	/// Default implementation of the ISuperintendentDataCore interface.
	/// </summary>
	/// <typeparam name="T">Controlls the instance's properties</typeparam>
	public class SuperintendentDataController<T> : ISuperintendentDataCore<T> where T : class
	{

		/// <summary>
		/// All operations with Memory cache should be made thread safe
		/// </summary>
		[SecurityCritical]
		private readonly IDictionary<string, object> _memoryCache = new Dictionary<string, object>();

		private readonly IDictionary<string, DataState> _persistenceStatus = new Dictionary<string, DataState>();

		private readonly IDictionary<string, string> _fileNameCache = new Dictionary<string, string>();

		private readonly string _currentControlledDataDirectory = DataControllerConfig.CreateDataDirectoryFromControlledData(typeof(T));

		private readonly T _currentInstance;

		public SuperintendentDataController(T instance)
		{
			_currentInstance = instance;
		}

		#region Private helpers

		#region FileName Cache

		/// <summary>
		/// Get a file name by PropertyInfo
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		private string GetFileNameForProperty(MemberInfo property)
		{
			return GetFileNameForProperty(property.Name);
		}

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

		private static bool RealSaveOperation(object state, string filename)
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
					Debug.WriteLine("Save operation failed. Making a retry: ({0})", retries);
				}
			}

			IsolatedStorageStringWriter.GetWriter.WriteStringToFile(jsonString, filename);

			return retries < 3 && !string.IsNullOrEmpty(filename);
		}

		#region Memory cache operators

		private readonly object _memoryCacheLock = new object();

		#endregion

		#region Property persistence status operators

		private readonly object _peristenceStatusLock = new object();

		private DataState GetPersistenceStatusForProperty(string propertyName)
		{
			var fileName = GetFileNameForProperty(propertyName);

			var stateResult = _persistenceStatus.GetValueFromDictionarySafe(propertyName, _peristenceStatusLock);

			if (stateResult == DataState.Unknown)
			{
				if (!_persistenceStatus.DictionaryContainsValueSafe(propertyName))
				{
					var result = IsolatedStorageBase.FileExists(fileName);
					stateResult = result ? DataState.Saved : DataState.Empty;
					_persistenceStatus.SetValueInDictionarySafe(propertyName, stateResult, _peristenceStatusLock);
				}
				else
				{
					throw new ArgumentOutOfRangeException("propertyName", "This property doesn't have a valid persistence status");
				}
			}

			return stateResult;
		}

		private void SetPersistanceStatusForProperty(string propertyName, DataState state)
		{
			var get = GetPersistenceStatusForProperty(propertyName);

			if (get != state)
			{
				_persistenceStatus[propertyName] = state;
			}

		}

		#endregion


		private object _getAccessorHandle = new object();
		
		private SuperindententDataObject<TE> GrabFromJson<TE>(string propertyName)
		{
			var fileNameString = GetFileNameForProperty(propertyName);
			var stringContents = IsolatedStorageStringWriter.GetWriter.ReadStringFromIsolatedStorage(fileNameString);

			SuperindententDataObject<TE> result;

			var persistState = GetPersistenceStatusForProperty(propertyName);

			switch (persistState)
			{
				case DataState.Empty:
					result = new SuperindententDataObject<TE>(SuperintendentStatus.NeedsReload, default(TE), propertyName);
					break;
				case DataState.Saved:
					try
					{
						var resultObject = JsonConvert.DeserializeObject<TE>(stringContents);
						_persistenceStatus[propertyName] = DataState.Saved;
						result = new SuperindententDataObject<TE>(SuperintendentStatus.StatusOk, resultObject, propertyName);
					}
					catch (Exception e)
					{
						result = new SuperindententDataObject<TE>(SuperintendentStatus.DataNotFound, default(TE), propertyName);
						Utils.ErrorLogInstance.AddError("GrabFromJsonSDC", e.Message);
					}
					break;
				case DataState.PendingWrite:
				case DataState.BeingWritten:
					result = new SuperindententDataObject<TE>(SuperintendentStatus.IsReloading, default(TE), propertyName);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		#endregion

		#region Implementation of ISuperintendentDataCore<T>

		public T ControlledInstance
		{
			get { return _currentInstance; }
		}

		public SuperindententDataObject<TE> RetrieveValue<TE>(string propertyName, TE defaultValue = default(TE))
		{
			SuperintendendentBase<T>.ContainsPropertyOfType(typeof(TE), propertyName);

			if (_memoryCache.DictionaryContainsValueSafe(propertyName))
			{
				var tempResult = _memoryCache.GetValueFromDictionarySafe(propertyName, _memoryCache, defaultValue);
				return new SuperindententDataObject<TE>(SuperintendentStatus.StatusOk, (TE)tempResult, propertyName);
			}


			/* this is from json*/

			var result = GrabFromJson<TE>(propertyName);
			var resultStatus = result.CurrentObjectStatus;

			switch (resultStatus)
			{
				case SuperintendentStatus.StatusOk:
					_memoryCache.SetValueInDictionarySafe(propertyName, result.Value, _memoryCacheLock);
					break;
				case SuperintendentStatus.Changed:
					if (!EqualityComparer<TE>.Default.Equals(defaultValue, default(TE)))
					{
						SaveValue(propertyName, defaultValue);
						result = new SuperindententDataObject<TE>(SuperintendentStatus.StatusOk, defaultValue, propertyName);
					}
					break;
				case SuperintendentStatus.NeedsReload:
				case SuperintendentStatus.IsReloading:
					result = new SuperindententDataObject<TE>(resultStatus, defaultValue, propertyName);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Debug.WriteLine("~ SuperintendentDataController REPORTS: Retrieving property \"{0}\"'s value from JSON. Future retrieves will go to memory.", propertyName);
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
			SuperintendendentBase<T>.ContainsPropertyOfType(typeof(TE), propertyName);

			Debug.Assert(propertyName != "CurrentServerType");
			/*
			
			 real save operations
			
			 */

			lock (this)
			{
				var getValue = RetrieveValue<TE>(propertyName).Value;
				if (EqualityComparer<TE>.Default.Equals(getValue, value))
				{
					Debug.WriteLine("Value is the same, skipping serialization!");
					GC.Collect();
					return;
				}
			}
			SetPersistanceStatusForProperty(propertyName, DataState.PendingWrite);
			_memoryCache.SetValueInDictionarySafe(propertyName, value, _memoryCacheLock);

			ThreadPool.QueueUserWorkItem(state =>
											{

												if (GetPersistenceStatusForProperty(propertyName) == DataState.PendingWrite)
												{
													SetPersistanceStatusForProperty(propertyName, DataState.BeingWritten);

													// These three can run concurrent...
													var getFileName = GetFileNameForProperty(propertyName);
													Debug.WriteLine("Saving property \"{0}\" to filename \"{1}\"", propertyName, getFileName);
													var result = RealSaveOperation(value, getFileName);

													// ...but this one must wait for the previous operation to complete.
													SetPersistanceStatusForProperty(propertyName, result ? DataState.Saved : DataState.Empty);
												}
												else
												{
													throw new InvalidOperationException("How did i get here?");
												}
											}, null);
		}

		#endregion
	}
}
