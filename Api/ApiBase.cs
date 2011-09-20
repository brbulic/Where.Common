using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using Where.Common.Services.Interfaces;

namespace Where.Api
{
	public delegate void DataLoadedCallback(bool error, Object o);

	public abstract class ApiBase
	{
		//Windows Phone 7 keycode
		public const String WhereWp7Keycode = "b86d118b-bb0a-4b1d-8591-295a0af80cbd";

		protected RequestType ApiRequestType = RequestType.Get;

		protected bool MarshallOnUiThread { get; set; }

		private static readonly IWebService WebRequestDispatcher = Utils.ConcurrentWebService;

		private readonly IDictionary<Guid, DataLoadedCallback> _concurrentRequests = new Dictionary<Guid, DataLoadedCallback>();

		protected ApiBase()
		{
			ApiRequestType = RequestType.Get;
			MarshallOnUiThread = true;
		}

		protected ApiBase(RequestType rt)
		{
			ApiRequestType = rt;
			MarshallOnUiThread = ApiRequestType == RequestType.Get;
		}

		private static DataLoadedCallback GetCallbackFromRequest(Guid key, IDictionary<Guid, DataLoadedCallback> callbacks)
		{
			if (callbacks.ContainsKey(key))
			{
				var callback = callbacks[key];
				callbacks.Remove(key);
				return callback;
			}

			return null;
		}

		public void CallDataLoadedCallback(Guid guid, bool isError, object userState)
		{
			DataLoadedCallback callback;

			lock (this)
			{
				callback = GetCallbackFromRequest(guid, _concurrentRequests);
				if (callback == null) return;
			}

			if (MarshallOnUiThread)
			{
				Deployment.Current.Dispatcher.BeginInvoke(() => callback(isError, userState));
			}
			else
			{
				callback(isError, userState);
			}


		}

		//private WebClient CreateWebClient()
		//{
		//    var wc = new WebClient();
		//    wc.Headers["user-agent"] = "Mozilla/4.0 (compatible; MSIE 7.0; Windows Phone OS 7.0; Trident/3.1; IEMobile/7.0; HTC; HD7)";
		//    wc.DownloadStringCompleted += ClientDownloadStringCompleted;
		//    return wc;
		//}


		/// <summary>
		/// Cancel all requests of the current instance (clear callbacks)
		/// </summary>
		public void CancelRequest()
		{
			lock (this)
			{
				foreach (var requests in _concurrentRequests)
				{
					var guid = requests.Key;
					Utils.ConcurrentWebService.CancelRequest(guid);
				}

				_concurrentRequests.Clear();
			}

		}

		/// <summary>
		/// Cancel a request by callback
		/// </summary>
		/// <param name="callback">Target callbacks </param>
		public void CancelRequest(DataLoadedCallback callback)
		{
			var cancellable = _concurrentRequests.Where(kvp => kvp.Value == callback).ToList();

			foreach (var keyValuePair in cancellable)
			{
				Utils.ConcurrentWebService.CancelRequest(keyValuePair.Key);
				_concurrentRequests.Remove(keyValuePair);
			}
		}

		protected void CallApi(String url, IDictionary<string, string> parameters, DataLoadedCallback callback, object userObject = null)
		{
			Guid guid;

			if (callback == null)
				throw new ArgumentNullException("callback", @"Callback for an ApiOperation MUST EXIST!");

			switch (ApiRequestType)
			{
				case RequestType.Get:
					guid = WebRequestDispatcher.InitiateGet(url, OnOperationCompleted, parameters, userObject);
					break;
				case RequestType.Post:
					guid = WebRequestDispatcher.InitiatePost(url, OnOperationCompleted, parameters, userObject);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			lock (this)
			{
				_concurrentRequests.Add(guid, callback);
			}
		}

		private void OnOperationCompleted(Guid guid, WebServiceEventArgs args)
		{
			var result = new ApiResult(args.UserState, args.Exception, args.IsHandled, args.ResponseString);
			ApiCallCompleted(guid, result);
		}

		public abstract void ApiCallCompleted(Guid guid, ApiResult e);

		~ApiBase()
		{
			Debug.WriteLine("~~~~~~ GC: {0} GC'd", GetType().Name);
		}

	}
}
