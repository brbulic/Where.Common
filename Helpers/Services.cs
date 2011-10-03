using Where.Common.Services;
using Where.Common.Services.Interfaces;

namespace Where
{
	/// <summary>
	/// The commonly used Utilities.
	/// </summary>
	public static partial class Utils
	{
		/// <summary>
		/// Experimental web service that allows two simultaneous web operations *UNSTABLE*
		/// </summary>
		public static IWebService DoubleRequestService
		{
			get { return WebService.Instance; }
		}

		public static ICacheService CacheService
		{
			get { return Common.Services.CacheService.Instance; }
		}

		/// <summary>
		/// Web service that handles a maximum of 5 concurrent requests. *STABLE*, use as default.
		/// </summary>
		public static IWebService ConcurrentWebService
		{
			get { return ConcurrentRequestWebService.Instance; }
		}

		/// <summary>
		/// Default web service. Uses <b>ConcurrentWebService</b> as default. Also available in the Utils class.
		/// </summary>
		public static IWebService DefaultWebService
		{
			get { return ConcurrentWebService; }
		}

		/// <summary>
		/// Check weather the device has internet connectivity.
		/// </summary>
		public static bool HasInternetConnectivity
		{
			get { return WebServiceBase.HasInternetConnectivity; }
		}

		private static IBackgroundDispatcher _backgroundDispatcherInstance;

		/// <summary>
		/// Default Background worker
		/// </summary>
		public static IBackgroundDispatcher BackgroundWorkerDefault
		{
			get
			{
				if (_backgroundDispatcherInstance == null)
					_backgroundDispatcherInstance = new BackgroundDispatcher(new NewThreadObjectQueue<IBackgroundOperationData>());

				return _backgroundDispatcherInstance;
			}
		}
	}
}
