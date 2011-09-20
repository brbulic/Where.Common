using System;
using System.ComponentModel;
using System.Net;
using Where.Api;

namespace Where.Common.Diagnostics
{
    public class ApiDebug
    {
        public const String ApiUrl = "http://www.appdebug.com/places/debugadd.php";

        private readonly DataLoadedCallback _callback;

        public ApiDebug(DataLoadedCallback callback)
        {
            _callback = callback;
        }

        public void DebugAdd(String user, String text)
        {
            if (String.IsNullOrEmpty(text)) return;
			
			var request = new WebClient();
            request.DownloadStringCompleted += DebugRequestCompleted;
            var urlString = string.Format("{0}?user={1}&debugtext={2}&ranval={3}", ApiUrl, user, text, Environment.TickCount);
            request.DownloadStringAsync(new Uri(urlString));
        }

        void DebugRequestCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            ((WebClient)sender).DownloadStringCompleted -= DebugRequestCompleted;
            ApiCallCompleted(e);
        }

        private void ApiCallCompleted(AsyncCompletedEventArgs e)
        {
            _callback(e.Error != null, string.Empty);
        }
    }
}
