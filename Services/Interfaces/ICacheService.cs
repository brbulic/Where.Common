using System;

namespace Where.Common.Services.Interfaces
{
    /// <summary>
    /// Cache service code contract for implementing application-wide object cache.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Check if a IWhereCacheable object exists
        /// </summary>
        /// <param name="key">The cacheable object</param>
        /// <returns></returns>
        bool Exists(IWhereCacheable key);

        /// <summary>
        /// Add an Cacheable object to cache
        /// </summary>
        /// <param name="value"></param>
        void AddToCache(IWhereCacheable value);

		/// <summary>
		/// Add an Cacheable object to cache. Only use DateTime to remove from cache, not hit count.
		/// </summary>
		/// <param name="value"></param>
		void AddToCachePersistent(IWhereCacheable value);

        /// <summary>
        /// Retrieve a cacheable object from cache
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="value">Cached object reference</param>
        /// <param name="defaultValue">Default cached object value (optional)</param>
        /// <returns>Cached object, if exists. Returns default reference doesn't exist</returns>
        T RetrieveFromCache<T>(IWhereCacheable value, T defaultValue = default(T)) where T : class, IWhereCacheable;

        /// <summary>
        /// Retrieve a Cacheable object from cache with an asynchronous operation
        /// </summary>
        /// <typeparam name="T">Cacheable object type</typeparam>
        /// <param name="value">Cacheable object value with key</param>
        /// <param name="defaultValue">If not found use default (optional)</param>
        /// <param name="callback">Async callback when retrieved from cache</param>
        void RetrieveFromCacheAsync<T>(IWhereCacheable value, Action<T> callback, T defaultValue = default(T)) where T : class, IWhereCacheable;
    }

    /// <summary>
    /// Implement this interface on object that you want cached
    /// </summary>
    public interface IWhereCacheable
    {
        /// <summary>
        /// Cache key
        /// </summary>
        string Key { get; }
    }
}
