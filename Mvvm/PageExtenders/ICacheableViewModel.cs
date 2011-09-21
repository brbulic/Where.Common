using Where.Common.Services.Interfaces;

namespace Where.Common.Mvvm.PageExtenders
{
	interface ICacheableViewModel<T> where T : IWhereCacheable
	{
		T LoadFromCache();

		void SaveToCache(T save);
	}
}
