using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using Where.Api;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	internal class WebService : WebServiceBase
	{

		#region Queues and collections

		private readonly BlockingThreadObjectQueue<DoubleQueueRequestData> _primaryWorker;

		private readonly BlockingThreadObjectQueue<DoubleQueueRequestData> _secondaryWorker;


		#endregion

		#region Private Helpers

		private volatile bool _isRunning;

		private enum CurrentlyUsedWorker
		{
			Primary,
			Secondary,
		}

		private CurrentlyUsedWorker _currentWorker = CurrentlyUsedWorker.Primary;


		#endregion

		private static IWebService _instance;

		internal static IWebService Instance
		{
			get { return _instance ?? (_instance = new WebService()); }
		}

		#region Implementation of IWebService

		private WebService()
		{
			_isRunning = true;
			_primaryWorker = new BlockingThreadObjectQueue<DoubleQueueRequestData>(StartApiOperation, WhatToDoOnFail, RunWhile);
			_primaryWorker.StartOperation();

			_secondaryWorker = new BlockingThreadObjectQueue<DoubleQueueRequestData>(StartApiOperation, WhatToDoOnFail, RunWhile);
			_secondaryWorker.StartOperation();

			Deployment.Current.Dispatcher.BeginInvoke(() => Application.Current.Exit += ApplicationExit);
		}

		void ApplicationExit(object sender, EventArgs e)
		{
			_isRunning = false;
			_primaryWorker.StopOperation();
			_secondaryWorker.StopOperation();
		}

		private bool RunWhile()
		{
			return _isRunning;
		}

		private void StartApiOperation(DoubleQueueRequestData arg1)
		{
			Executor(arg1);
		}

		private void WhatToDoOnFail(DoubleQueueRequestData obj)
		{
			if (obj.IsHandled) return;

			Responser(obj, new WebServiceEventArgs(WebApiServerStatus.Error, new Exception(), String.Empty, obj.UserState));
		}


		/// <summary>
		/// Initiate a Request cancellation, will immediately call the WebServiceResult with an empty response
		/// </summary>
		/// <param name="guid">Guid of the request</param>
		/// <returns>Notifies the user if the request cancellation is under way</returns>
		public override bool CancelRequest(Guid guid)
		{
			bool win;

			// check if primary has pending
			if (_primaryWorker.OperationPending(value => value.ApiGuidToken == guid))
			{
				win = _primaryWorker.CancelOperation(value => value.ApiGuidToken == guid);
				return win;
			}

			// check if secondary has pending
			if (_secondaryWorker.OperationPending(value => value.ApiGuidToken == guid))
			{
				win = _secondaryWorker.CancelOperation(value => value.ApiGuidToken == guid);
				return win;
			}

			// check if already running...
			win = base.RequestRunning(guid);

			return win;
		}

		public override Guid InitiateGet(string url, WebServiceResult result, IDictionary<string, string> parameters, object userState)
		{
			var guid = Guid.NewGuid();

			var pars = Utils.ProcessArguments(parameters);
			var requestItem = new DoubleQueueRequestData(guid, RequestType.Get, url, pars, userState, result, _currentWorker);

			ThreadPool.QueueUserWorkItem(state =>
											 {
												 var item = (DoubleQueueRequestData)state;

												 switch (_currentWorker)
												 {
													 case CurrentlyUsedWorker.Primary:
														 _primaryWorker.EnqueueOperation(item);
														 _currentWorker = CurrentlyUsedWorker.Secondary;
														 break;
													 case CurrentlyUsedWorker.Secondary:
														 _secondaryWorker.EnqueueOperation(item);
														 _currentWorker = CurrentlyUsedWorker.Primary;
														 break;
													 default:
														 throw new ArgumentOutOfRangeException();
												 }
											 }, requestItem);
			return guid;
		}

		public override Guid InitiatePost(string url, WebServiceResult result, string writableString, object userState)
		{
			var guid = Guid.NewGuid();

			var requestItem = new DoubleQueueRequestData(guid, RequestType.Post, url, writableString, userState, result, _currentWorker);

			switch (_currentWorker)
			{
				case CurrentlyUsedWorker.Primary:
					_primaryWorker.EnqueueOperation(requestItem);
					_currentWorker = CurrentlyUsedWorker.Secondary;
					break;
				case CurrentlyUsedWorker.Secondary:
					_secondaryWorker.EnqueueOperation(requestItem);
					_currentWorker = CurrentlyUsedWorker.Primary;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return guid;
		}

		#endregion

		#region Executors

		private void Executor(DoubleQueueRequestData requestData)
		{
			Debug.Assert(requestData.Result != null);
			Debug.Assert(HasInternetConnectivity);

			if (requestData.RequestType == RequestType.Get)
			{
				CreateGetRequestWithState(requestData, GetResponseProcessorCallback);
			}
			else
			{
				CreatePostRequestWithState(requestData, StartPostRequestStreamCallback);
			}


		}

		#region GET

		private void GetResponseProcessorCallback(IAsyncResult ar)
		{
			var webState = (PrivateWebUserState)ar.AsyncState;

			HttpWebResponse response = null;
			var result = WebServiceEventArgs.CreateDefault(webState.Data.UserState);

			try
			{
				response = (HttpWebResponse)webState.Request.EndGetResponse(ar);
				using (var returnStream = response.GetResponseStream())
				using (var reader = new StreamReader(returnStream))
				{
					var builder = new StringBuilder();

					while (!reader.EndOfStream)
						builder.Append(reader.ReadLine());

					var resultString = builder.ToString();
					result = new WebServiceEventArgs(WebApiServerStatus.Success, null, resultString, webState.Data.UserState);
				}
			}
			catch (Exception e)
			{
				if (e is WebException)
					result = GenerateFromException(e as WebException, HasInternetConnectivity, webState.Data.UserState);
				else
					result = new WebServiceEventArgs(WebApiServerStatus.InvalidStream, e, String.Empty, webState.Data.UserState);

			}
			finally
			{
				if (response != null)
					response.Close();

				Responser(webState.Data, result);
			}
		}

		#endregion

		#region POST

		private void StartPostRequestStreamCallback(IAsyncResult ar)
		{
			var userState = (PrivateWebUserState)ar.AsyncState;

			Stream writableStream = null;

			try
			{
				writableStream = userState.Request.EndGetRequestStream(ar);
				using (var memoryStream = new MemoryStream())
				{
					var writableString = userState.Data.Parameters;
					if (userState.Request.ContentType == PostContentType)
					{
						writableString = writableString.TrimStart('?');
					}

					// write tu buffer and reset position
					var getBytes = Encoding.UTF8.GetBytes(writableString);
					memoryStream.Write(getBytes, 0, getBytes.Length);
					memoryStream.Position = 0;

					// read from buffer stream
					var buffer = new byte[getBytes.Length];
					memoryStream.Read(buffer, 0, buffer.Length);

					// write to stream
					writableStream.Write(buffer, 0, buffer.Length);
				}
			}
			catch (Exception e)
			{
				WebServiceEventArgs result;

				if (e is WebException)
					result = GenerateFromException(e as WebException, HasInternetConnectivity, userState.Data.UserState);
				else
					result = new WebServiceEventArgs(WebApiServerStatus.InvalidStream, e, String.Empty, userState.Data.UserState);

				Responser(userState.Data, result);
				return;
			}
			finally
			{
				if (writableStream != null)
				{
					writableStream.Close();
					writableStream.Dispose();

					// If all done good, get the response from the server
					userState.Request.BeginGetResponse(GetResponseProcessorCallback, userState);
				}
			}

		}

		#endregion

		#endregion

		#region Responser



		protected override void Responser(PrivateRequestData requestData, WebServiceEventArgs completer)
		{
			var myRequestData = (DoubleQueueRequestData)requestData;

			switch (myRequestData.CurrentWorker)
			{
				case CurrentlyUsedWorker.Primary:
					_primaryWorker.SignalOperationDone();
					break;
				case CurrentlyUsedWorker.Secondary:
					_secondaryWorker.SignalOperationDone();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			base.Responser(myRequestData, completer);
		}

		#endregion

		#region Private Helpers

		private sealed class DoubleQueueRequestData : PrivateRequestData
		{

			private readonly CurrentlyUsedWorker _currentWorker;

			public DoubleQueueRequestData(Guid apiGuidToken, RequestType requestType, string requestUri, string parameters, object userState, WebServiceResult result, CurrentlyUsedWorker currentWorker, bool returnOnUi = false)
				: base(apiGuidToken, requestType, requestUri, parameters, userState, result, returnOnUi)
			{

				_currentWorker = currentWorker;

			}

			public CurrentlyUsedWorker CurrentWorker
			{
				get
				{
					ThrowExeption();
					return _currentWorker;
				}
			}


			~DoubleQueueRequestData()
			{
				Debug.WriteLine("====> WEB GC: DoubleQueueRequestData data with guid {0} GC'd", ApiGuidToken);
			}

		}

		#endregion
	}
}
