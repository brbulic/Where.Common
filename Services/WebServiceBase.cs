using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using Where.Api;
using Where.Common.Diagnostics;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	internal abstract class WebServiceBase : IWebService
	{
		private readonly IDictionary<Guid, WebRequest> _runningRequests = new Dictionary<Guid, WebRequest>();

		#region Protected properties

		protected IDictionary<Guid, WebRequest> RunningRequests
		{
			get
			{
				return _runningRequests;
			}
		}

		#endregion

		#region Implementation of IWebService

		public static bool HasInternetConnectivity
		{
			get { return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable(); }
		}

		/// <summary>
		/// Check if a request is running
		/// </summary>
		/// <param name="guid">Guid of your request</param>
		/// <returns>Notifies the caller that the request is running</returns>
		public virtual bool RequestRunning(Guid guid)
		{
			bool running;

			lock (_requestLockObject)
			{
				running = _runningRequests.Any(data => data.Key == guid);
			}

			return running;
		}

		/// <summary>
		/// Initiate a Request cancellation, will immediately call the WebServiceResult with an empty response
		/// </summary>
		/// <param name="guid">Guid of the request</param>
		/// <returns>Notifies the user if the request cancellation is under way</returns>
		public abstract bool CancelRequest(Guid guid);

		/// <summary>
		/// Initiate a GET request, returns result string in the WebServiceResult callback
		/// </summary>
		/// <param name="url">Get URL</param>
		/// <param name="result">Response delegate</param>
		/// <param name="parameters">Get parameters (if any)</param>
		/// <param name="userState">User state (if any) </param>
		/// <returns>Guid of the request</returns>
		public abstract Guid InitiateGet(string url, WebServiceResult result, IDictionary<string, string> parameters, object userState);

		/// <summary>
		/// Initiate a POST request with an string to upload, returns the result string in the WebServiceResult callback
		/// </summary>
		/// <param name="url">Get URL</param>
		/// <param name="result">Response delegate</param>
		/// <param name="writableString">String to upload</param>
		/// <param name="userState">User state (if any)</param>
		/// <returns>Guid of the Request</returns>
		public abstract Guid InitiatePost(string url, WebServiceResult result, string writableString, object userState);

		/// <summary>
		/// Initiate a POST request with parameters to upload, return the result string in the WebServiceResult callback
		/// </summary>
		/// <param name="url">Get URL</param>
		/// <param name="result">Response delegate</param>
		/// <param name="parameters">Parameters to upload</param>
		/// <param name="userState">User state object (if any)</param>
		/// <returns>Guid of the Request</returns>
		public Guid InitiatePost(string url, WebServiceResult result, IDictionary<string, string> parameters, object userState)
		{
			return InitiatePost(url, result, ArgumentsProcessor(parameters), userState);
		}

		#endregion

		#region Initiators

		internal const string PostContentType = "application/x-www-form-urlencoded";

		protected void CreateGetRequestWithState(PrivateRequestData requestData, AsyncCallback getCallback)
		{
			if (requestData == null)
				throw new ArgumentNullException("requestData", @"Must have request data to perform a request. Loser.");

			if (getCallback == null)
				throw new ArgumentNullException("getCallback", @"Must have a callback to return results for requested data. Loser.");


			if (!HasInternetConnectivity)
			{
				Debug.WriteLine("NO INTERNET connectivity, skipping");
				return;
			}

			var urlGenerated = string.Format("{0}{1}", requestData.RequestUri, requestData.Parameters);
			var request = WebRequest.CreateHttp(urlGenerated);

			request.AllowReadStreamBuffering = true;
			request.Method = "GET";

			request.BeginGetResponse(getCallback, new PrivateWebUserState(request, requestData));
			WhereDebug.WriteLine(string.Format("API GET: {0}", urlGenerated));

			lock (_requestLockObject)
			{
				_runningRequests.Add(requestData.ApiGuidToken, request);
			}

		}

		protected void CreatePostRequestWithState(PrivateRequestData requestData, AsyncCallback postCallback)
		{
			if (requestData == null)
				throw new ArgumentNullException("requestData", @"Must have request data to perform a request. Loser.");

			if (postCallback == null)
				throw new ArgumentNullException("postCallback", @"Must have a callback to return results for requested data. Loser.");

			if (!HasInternetConnectivity)
			{
				Debug.WriteLine("NO INTERNET connectivity, skipping");
				return;
			}

			var request = WebRequest.CreateHttp(requestData.RequestUri);
			request.Method = "POST";

			if (requestData.Parameters.Contains("?"))
				request.ContentType = PostContentType;

			request.BeginGetRequestStream(postCallback, new PrivateWebUserState(request, requestData));
			WhereDebug.WriteLine(string.Format("API POST: {0}, writing string {1}", requestData.RequestUri, requestData.Parameters));

			lock (_requestLockObject)
			{
				_runningRequests.Add(requestData.ApiGuidToken, request);
			}

		}

		#endregion

		#region Complete and send async callback

		private readonly object _requestLockObject = new object();

		/// <summary>
		/// Call the response callback from WebServiceEventArgs
		/// </summary>
		/// <param name="requestData"></param>
		/// <param name="completer"></param>
		protected virtual void Responser(PrivateRequestData requestData, WebServiceEventArgs completer)
		{
			if (completer == null)
				throw new ArgumentNullException("completer", "Response must not be null.");

			if (completer.ResponseStatus == WebApiServerStatus.Unknown)
				Utils.ErrorLogInstance.AddError("Responser", "Default WebServiceEventArgs found. Invalid Web state");

			lock (_requestLockObject)
			{
				// remove from running requests
				RunningRequests.Remove(requestData.ApiGuidToken);
			}

			if (requestData.IsHandled) return;

			// Finally call the callback on the father thread
			if (requestData.ReturnOnUi)
				Deployment.Current.Dispatcher.BeginInvoke(() => requestData.Result(requestData.ApiGuidToken, completer));
			else
				requestData.Result(requestData.ApiGuidToken, completer);

			requestData.Dispose();
		}


		#endregion

		#region Internal Helpers

		protected static WebServiceEventArgs GenerateFromException(WebException e, bool hasInternet, object userState)
		{
			WebServiceEventArgs result;
			var webEx = e;

			Debug.WriteLine("WebException status {0}", webEx.Status);

			switch (e.Status)
			{
				case WebExceptionStatus.RequestCanceled:
					Debug.WriteLine("Cancelled!");
					result = new WebServiceEventArgs(WebApiServerStatus.Cancelled, e, String.Empty, userState);
					break;
				case WebExceptionStatus.ConnectFailure:
					Debug.WriteLine("Invalid Web Address");
					result = new WebServiceEventArgs(WebApiServerStatus.Error, e, String.Empty, userState);
					break;
				default:
					Debug.WriteLine("Some crappy error");
					result = !hasInternet ?
						new WebServiceEventArgs(WebApiServerStatus.NoConnection, e, String.Empty, userState) :
						new WebServiceEventArgs(WebApiServerStatus.Unknown, e, String.Empty, userState);
					break;
			}

			if (e.Message.Contains("NotFound") && hasInternet)
			{
				result = new WebServiceEventArgs(WebApiServerStatus.InvalidUrl, e, String.Empty, userState);
			}

			return result;
		}

		private static string ArgumentsProcessor(IDictionary<string, string> args)
		{
			var builder = new StringBuilder();
			return Utils.ProcessArguments(args, true, builder);
		}

		protected static PrivateRequestData CreateGetRequestData(string url, WebServiceResult result, IDictionary<string, string> args, object userState = null, bool onUi = false)
		{
			return PrivateRequestData.CreateRequest(RequestType.Get, url, ArgumentsProcessor(args), userState, result, onUi);
		}

		protected static PrivateRequestData CreatePostRequestData(string url, WebServiceResult result, IDictionary<string, string> args, object userState = null, bool onUi = false)
		{
			return PrivateRequestData.CreateRequest(RequestType.Post, url, ArgumentsProcessor(args), userState, result, onUi);
		}

		protected static PrivateRequestData CreatePostRequestDataFromString(string url, WebServiceResult result, string args, object userState = null, bool onUi = false)
		{
			return PrivateRequestData.CreateRequest(RequestType.Post, url, args, userState, result, onUi);
		}

		#endregion

		#region Private Classes

		/// <summary>
		/// Internal class that contains data about a single request. On completion, the class is cleaned.
		/// </summary>
		protected class PrivateRequestData : IDisposable
		{

			private readonly Guid _apiGuidToken;
			private readonly RequestType _requestType;
			private readonly bool _returnOnUi;

			private WebServiceResult _result;
			private string _requestUri;
			private object _userState;
			private string _parameters;

			public bool IsHandled { get; private set; }


			public static PrivateRequestData CreateRequest(RequestType requestType, string requestUri, string args, object userState, WebServiceResult result, bool returnOnUi = false)
			{
				return new PrivateRequestData(Guid.NewGuid(), requestType, requestUri, args, userState, result, returnOnUi);
			}

			protected PrivateRequestData(Guid apiGuidToken, RequestType requestType, string requestUri, string parameters, object userState, WebServiceResult result, bool returnOnUi = false)
			{
				_apiGuidToken = apiGuidToken;
				_result = result;
				_returnOnUi = returnOnUi;
				_userState = userState;
				_parameters = parameters;
				_requestUri = requestUri;
				_requestType = requestType;
			}

			public bool ReturnOnUi
			{
				get
				{
					ThrowExeption();
					return _returnOnUi;
				}
			}

			public WebServiceResult Result
			{
				get
				{
					ThrowExeption();
					return _result;
				}
			}

			public object UserState
			{
				get
				{
					ThrowExeption();
					return _userState;
				}
			}

			public string Parameters
			{
				get
				{
					ThrowExeption();
					return _parameters;
				}
			}

			public string RequestUri
			{
				get
				{
					ThrowExeption();
					return _requestUri;
				}
			}

			public RequestType RequestType
			{
				get
				{
					ThrowExeption();
					return _requestType;
				}
			}

			public Guid ApiGuidToken
			{
				get
				{
					ThrowExeption();
					return _apiGuidToken;
				}
			}


			#region IDisposable

			private bool _disposed;

			public virtual void Dispose()
			{
				if (_disposed)
					throw new ObjectDisposedException("PrivateRequestData", "Cannot dispose a disposed request");

				GC.SuppressFinalize(this);

				Debug.WriteLine("====> WEB PRE-GC: PrivateRequestData GUID: {0} disposed!", _apiGuidToken);

				_userState = null;
				_result = null;
				_requestUri = null;
				_parameters = null;
				_disposed = true;
				IsHandled = true;

				GC.ReRegisterForFinalize(this);
				GC.Collect();
			}

			protected void ThrowExeption()
			{
				if (_disposed)
					throw new ObjectDisposedException("PrivateRequestData", "You cannot reuse a same request");
			}

			#endregion // End of IDisposeable
		}

		protected class PrivateWebUserState
		{
			private readonly WebRequest _request;
			private readonly PrivateRequestData _data;

			public PrivateWebUserState(WebRequest request, PrivateRequestData data)
			{
				_request = request;
				_data = data;
			}

			public PrivateRequestData Data
			{
				get { return _data; }
			}

			public WebRequest Request
			{
				get { return _request; }
			}

			~PrivateWebUserState()
			{
				Debug.WriteLine("====> WEB GC: PrivateWebUserState with hash:{0} GC'd", GetHashCode());
			}
		}


		#endregion

	}
}
