using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Where.Common.Diagnostics;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	public sealed class CacheService : ICacheService
	{
		private const string DirectoryPath = "ObjectCache";

		#region Singleton config

		private static ICacheService _localCopy;

		public static ICacheService Instance
		{
			get
			{
				return _localCopy ?? (_localCopy = new CacheService());
			}
		}

		#endregion

		#region Cache internals

		/// <summary>
		/// Holds cached items per key
		/// </summary>
		private readonly IDictionary<string, WeakReference<object>> _objectMemoryCache = new Dictionary<string, WeakReference<object>>();

		/// <summary>
		/// Holds cache history per key
		/// </summary>
		private readonly IDictionary<string, ObjectCacheData> _objectCacheData = new Dictionary<string, ObjectCacheData>();

		#endregion

		#region Misc

		private const string CacheDataFileName = "CacheData.json";

		private DispatcherTimer _timer;

		private readonly IsolatedStorageJson<Dictionary<string, ObjectCacheData>> _cacheDataAccess = new IsolatedStorageJson<Dictionary<string, ObjectCacheData>>();

		private readonly IsolatedStorageJson<string> _cacheIsolatedStorageJsonWriter = new IsolatedStorageJson<string>();


		private void SaveCachedItems(object sender, EventArgs e)
		{
			_cacheDataAccess.SyncSaveDataToJson(CacheDataFileName, (Dictionary<string, ObjectCacheData>)_objectCacheData);
			System.Diagnostics.Debug.WriteLine("Saved {0} cached items...", _objectCacheData.Count);
			_timer.Stop();
		}

		private void RestoreCacheItems()
		{
			var cd = _cacheDataAccess.LoadFromFile(CacheDataFileName);

			if (cd != null)
				foreach (var o in cd.Where(o => !string.IsNullOrEmpty(o.Value.JsonFileName)))
				{
					_objectCacheData.Add(o);
				}

			CleanAndResyncCache(null, null);
		}

		private void CleanAndResyncCache(object sender, EventArgs e)
		{
			var getObsolete = (from cacheKey in _objectCacheData
							   where cacheKey.Value.MarkForDeletetion
							   select cacheKey).ToList();

			var count = 0;
			foreach (var keyValuePair in from keyValuePair in getObsolete let win = IsolatedStorageBase.DeleteFile(keyValuePair.Value.JsonFileName) where win select keyValuePair)
			{
				count++;
				WhereDebug.WriteLine(string.Format("Removed item with key:{0}, last access {1}, times accessed {2}", keyValuePair.Key, keyValuePair.Value.LastAccess, keyValuePair.Value.CacheHit));
				_objectCacheData.Remove(keyValuePair);
				_objectMemoryCache.Remove(keyValuePair.Key);
			}

			System.Diagnostics.Debug.WriteLine("Removed {0} instances from cache in this sweep.", count);

		}

		#endregion

		private CacheService()
		{
			RestoreCacheItems();
			System.Diagnostics.Debug.WriteLine("CacheService first call!");

			Deployment.Current.Dispatcher.BeginInvoke(() =>
														  {
															  Application.Current.Exit += SaveCachedItems;
															  _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
															  _timer.Tick += CleanAndResyncCache;
															  _timer.Start();
														  });



			using (var current = IsolatedStorageFile.GetUserStoreForApplication())
			{
				if (!current.DirectoryExists(DirectoryPath))
					current.CreateDirectory(DirectoryPath);
			}
		}

		#region Implementation of ICacheService

		public bool Exists(IWhereCacheable key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return _objectCacheData.ContainsKey(key.Key);
		}

		public void AddToCache(IWhereCacheable value)
		{
			RealAddToCacheWorker(value, false);
		}

		private void RealAddToCacheWorker(IWhereCacheable value, bool persistent)
		{
			if (Exists(value))
			{
				if (_objectMemoryCache.ContainsKey(value.Key))
				{
					var currentObject = _objectMemoryCache[value.Key];

					if (!currentObject.IsAlive)
						_objectMemoryCache[value.Key] = new WeakReference<object>(value);
				}
				else
				{
					_objectMemoryCache.Add(value.Key, new WeakReference<object>(value));
				}

				var cacheData = _objectCacheData[value.Key];
				cacheData.CacheHit++;
				cacheData.LastAccess = DateTime.Now;

			}
			else
			{

				var fileName = string.Format("ObjectCache\\{0}.json", value.Key);

				// Json serializer is already async
				_cacheIsolatedStorageJsonWriter.SaveDataToJson(fileName, JsonConvert.SerializeObject(value));

				if (_objectCacheData.ContainsKey(value.Key))
				{
					_objectCacheData[value.Key].JsonFileName = fileName;
					return;
				}

				_objectCacheData.Add(value.Key, new ObjectCacheData { DateAdded = DateTime.Now, LastAccess = DateTime.Now, JsonFileName = fileName, CacheHit = persistent ? 99 : 0 });
				_objectMemoryCache.Add(value.Key, new WeakReference<object>(value));
			}

		}

		public void AddToCachePersistent(IWhereCacheable value)
		{
			RealAddToCacheWorker(value, true);
		}

		public T RetrieveFromCache<T>(IWhereCacheable value, T defaultValue = default(T)) where T : class, IWhereCacheable
		{
			if (Exists(value))
			{
				T returnValue = default(T);
				var hasInMemory = false;

				var cacheData = _objectCacheData[value.Key];

				if (_objectMemoryCache.ContainsKey(value.Key))
				{
					var cachedValue = _objectMemoryCache[value.Key];
					if (cachedValue.IsAlive)
					{
						hasInMemory = cachedValue.IsAlive;

						if (cachedValue.Target is T)
							returnValue = (T)cachedValue.Target;
					}
				}

				if (!hasInMemory)
				{
					if (string.IsNullOrEmpty(cacheData.JsonFileName))
					{
						_objectCacheData.Remove(value.Key);
						return defaultValue;
					}

					var jsonString = _cacheIsolatedStorageJsonWriter.LoadFromFile(cacheData.JsonFileName);
					var deserializiedValue = default(T);

					try
					{
						if (string.IsNullOrEmpty(jsonString))
						{
							// Cache data is invalid, file either doesn't exist or is empty
							IsolatedStorageBase.DeleteFile(cacheData.JsonFileName);
							_objectMemoryCache.Remove(value.Key);
							_objectCacheData.Remove(value.Key);
							return defaultValue;
						}

						deserializiedValue = JsonConvert.DeserializeObject<T>(jsonString);
						_objectMemoryCache[value.Key] = new WeakReference<object>(deserializiedValue);
					}
					catch (Exception e)
					{
						System.Diagnostics.Debug.WriteLine("Error in json cache deserialization! Error:{0}", e.Message);
						_objectCacheData.Remove(value.Key);
						_objectMemoryCache.Remove(value.Key);
						return deserializiedValue;
					}

					returnValue = deserializiedValue;
				}

				/* Debug data */
				WhereDebug.WriteLine(string.Format("Cache hit for key: {0}, ObjectType: {1}, LastAccess {2:f}. From json? {3}", value.Key, value.GetType().Name, _objectCacheData[value.Key].LastAccess, !hasInMemory));

				_objectCacheData[value.Key].CacheHit++;
				_objectCacheData[value.Key].LastAccess = DateTime.Now;


				return returnValue;

			}

			return defaultValue;
		}

		/// <summary>
		/// Retrieve a Cacheable object from cache with an asynchronous operation
		/// </summary>
		/// <typeparam name="T">Cacheable object type</typeparam>
		/// <param name="value">Cacheable object value with key</param>
		/// <param name="defaultValue">If not found use default (optional)</param>
		/// <param name="callback">Async callback when retrieved from cache</param>
		public void RetrieveFromCacheAsync<T>(IWhereCacheable value, Action<T> callback, T defaultValue = default(T)) where T : class, IWhereCacheable
		{
			ThreadPool.QueueUserWorkItem(state =>
											 {
												 Thread.CurrentThread.Name = "CacheRetrievalThreadPoolMember";
												 lock (_objectCacheData)
												 {
													 var result = RetrieveFromCache(value, defaultValue);
													 Deployment.Current.Dispatcher.BeginInvoke(() => callback(result));
												 }
											 }, null);
		}

		#endregion

		private static readonly TimeSpan CacheTimeout = TimeSpan.FromDays(2);

		/// <summary>
		/// Cache that has the hit ratio
		/// </summary>
		internal class ObjectCacheData
		{
			public ObjectCacheData()
			{
				CacheHit = 0;
			}

			public int CacheHit { get; set; }

			public DateTime DateAdded { get; set; }

			public DateTime LastAccess { get; set; }

			public string JsonFileName { get; set; }

			public bool MarkForDeletetion
			{
				get
				{
					if (CacheHit == 0)
						return true;

					var timePassed = DateTime.Now.Subtract(LastAccess);
					return timePassed > CacheTimeout;
				}
			}
		}

	}
}