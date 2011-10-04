using System;
using System.Collections.Generic;
using System.Net;
using Where.Api;

namespace Where.Common.Diagnostics
{
	public class ApiDebug
	{
		public const String ApiUrl = "http://www.appdebug.com/places/debugadd.php";


		public readonly IDictionary<Guid, DataLoadedCallback> _callback = new Dictionary<Guid, DataLoadedCallback>();

		private DataLoadedCallback GetCallbackPerUserId(Guid guid)
		{
			if (_callback.ContainsKey(guid))
			{
				var callback = _callback[guid];
				_callback.Remove(guid);
				return callback;
			}

			return null;
		}

		public void DebugAdd(String user, String text, DataLoadedCallback callback)
		{
			if (String.IsNullOrEmpty(text)) return;

			var urlString = string.Format("{0}?user={1}&debugtext={2}&ranval={3}", ApiUrl, user, text, Environment.TickCount);

			var webRequest = new WebClient();
			webRequest.DownloadStringCompleted += WebRequestDownloadStringCompleted;
			var guid = Guid.NewGuid();
			_callback.Add(guid, callback);
			webRequest.DownloadStringAsync(new Uri(urlString), guid);
		}

		void WebRequestDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			var guid = (Guid)e.UserState;
			var callback = GetCallbackPerUserId(guid);

			if (callback == null) throw new InvalidOperationException("Cannot send a callback if the request isn't found. WTF?");
			callback(e.Error != null, e.Error);
		}

	}
}
