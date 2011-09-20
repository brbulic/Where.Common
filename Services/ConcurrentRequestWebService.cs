using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Where.Api;
using System.Linq;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	/// <summary>
	/// Implementation of the IWebService that can run and process 5 concurrent requests
	/// </summary>
	internal class ConcurrentRequestWebService : WebServiceBase
	{
		/// <summary>
		/// Returns true if running
		/// </summary>
		public bool IsRunning { get; private set; }

		#region Queues

		private readonly Queue<PrivateRequestData> _queuedRequest = new Queue<PrivateRequestData>();

		private readonly Queue<IAsyncResult> _pendingProcessing = new Queue<IAsyncResult>();

		#endregion

		#region Private helpers

		private static readonly object ApiLockSync = new object();

		private const int MaximumConcurrentRequests = 5;

		private Thread _workerThread;

		#endregion

		private static IWebService _instance;

		public static IWebService Instance
		{
			get { return _instance ?? (_instance = new ConcurrentRequestWebService()); }
		}
		
		private ConcurrentRequestWebService()
		{
			Start();
		}
		
		private void Start()
		{
			IsRunning = true;
			_workerThread = new Thread(ThreadWorker);
			_workerThread.Start();
		}

		public void Stop()
		{
			lock (ApiLockSync)
			{
				IsRunning = false;
				Monitor.Pulse(ApiLockSync);
			}
		}

		private void ThreadWorker()
		{
			Thread.CurrentThread.Name = "ConcurrentRequestWebService-" + GetHashCode();

			var pendingRequests = new Queue<PrivateRequestData>();
			var pendingResponses = new Queue<IAsyncResult>();

			while (IsRunning)
			{
				lock (ApiLockSync)
				{
					if (_queuedRequest.Count == 0 && _pendingProcessing.Count == 0 && pendingRequests.Count == 0 && pendingResponses.Count == 0)
					{
						Monitor.Wait(ApiLockSync);

						if (!IsRunning)
							break;
					}

					while (_queuedRequest.Count > 0)
					{
						pendingRequests.Enqueue(_queuedRequest.Dequeue());
					}

					while (_pendingProcessing.Count > 0)
					{
						pendingResponses.Enqueue(_pendingProcessing.Dequeue());
					}
				}


				var pendingCompletion = new Queue<PendingCompletion>();

				for (var i = 0; (i < pendingRequests.Count) && (i < MaximumConcurrentRequests); i++)
				{
					var currentData = pendingRequests.Dequeue();

					if (currentData.RequestType == RequestType.Get)
						CreateGetRequestWithState(currentData, ProcessorCallback);
					else if (currentData.RequestType == RequestType.Post)
						CreatePostRequestWithState(currentData, ProcessorCallbackPost);

					Thread.Sleep(1);
				}

				for (var i = 0; (i < pendingResponses.Count) && (i < MaximumConcurrentRequests); i++)
				{
					var pendingResponse = pendingResponses.Dequeue();

					var pending = ProcessCompletion(pendingResponse);
					pendingCompletion.Enqueue(pending);

					Thread.Sleep(1);

				}

				for (var i = 0; i < pendingCompletion.Count; i++)
				{
					var almostDone = pendingCompletion.Dequeue();

					PrivateCompletionProcessor(almostDone);

					Thread.Sleep(1);
				}
			}
		}


		private static readonly StringBuilder StringBuilder = new StringBuilder();

		private void PrivateCompletionProcessor(PendingCompletion completion)
		{
			if (completion.Error != null)
			{
				WebServiceEventArgs response;

				if (completion.Error is WebException)
				{
					response = GenerateFromException(completion.Error as WebException, HasInternetConnectivity, completion.RequestData.UserState);
				}
				else
				{
					response = new WebServiceEventArgs(WebApiServerStatus.Error, completion.Error, String.Empty, completion.RequestData.UserState);
				}

				Responser(completion.RequestData, response);
			}
			else // Read data from stream and continue
			{
				WebServiceEventArgs finalResponse;

				using (var stream = completion.Result)
				{
					using (var streamReader = new StreamReader(stream))
					{
						while (!streamReader.EndOfStream)
						{
							StringBuilder.Append(streamReader.ReadLine());
						}

						var stringResult = StringBuilder.ToString();
						finalResponse = new WebServiceEventArgs(WebApiServerStatus.Success, null, stringResult, completion.RequestData.UserState);
					}
				}

				completion.Response.Close();
				StringBuilder.Flush();

				Responser(completion.RequestData, finalResponse);
			}
		}

		private static PendingCompletion ProcessCompletion(IAsyncResult pendingResponse)
		{

			HttpWebResponse response = null;
			PendingCompletion completed;
			var responseState = (PrivateWebUserState)pendingResponse.AsyncState;

			try
			{
				response = (HttpWebResponse)responseState.Request.EndGetResponse(pendingResponse);
				var stream = response.GetResponseStream();
				completed = new PendingCompletion(responseState.Data, stream, null, response);
			}
			catch (Exception error)
			{
				if (response != null)
					response.Close();

				completed = new PendingCompletion(responseState.Data, null, error, null);
			}

			return completed;
		}

		private void ProcessorCallbackPost(IAsyncResult ar)
		{
			var userState = (PrivateWebUserState)ar.AsyncState;

			Stream writableStream = null;
			try
			{
				writableStream = userState.Request.EndGetRequestStream(ar);


				using (var memoryStream = new MemoryStream())
				{
					var writableString = userState.Data.Parameters;

					writableString = writableString.TrimStart('?');

					// write to buffer and reset position
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
				if (e is WebException)
				{
					lock (ApiLockSync)
					{
						_pendingProcessing.Enqueue(ar);
						Monitor.Pulse(ApiLockSync);
					}
				}
			}
			finally
			{
				if (writableStream != null)
				{
					writableStream.Close();
					writableStream.Dispose();

					userState.Request.BeginGetResponse(ProcessorCallback, userState);
				}
			}

		}

		private void ProcessorCallback(IAsyncResult ar)
		{
			lock (ApiLockSync)
			{
				_pendingProcessing.Enqueue(ar);
				Monitor.Pulse(ApiLockSync);
			}
		}

		#region Private helpers


		private class PendingCompletion
		{
			private readonly PrivateRequestData _requestData;
			private readonly Stream _result;
			private readonly Exception _error;
			private readonly HttpWebResponse _response;

			public PendingCompletion(PrivateRequestData requestData, Stream result, Exception error, HttpWebResponse response)
			{
				_requestData = requestData;
				_error = error;
				_response = response;
				_result = result;
			}

			public HttpWebResponse Response
			{
				get { return _response; }
			}

			public Exception Error
			{
				get { return _error; }
			}

			public Stream Result
			{
				get { return _result; }
			}

			public PrivateRequestData RequestData
			{
				get { return _requestData; }
			}
		}


		#endregion

		#region Implementation of IWebService

		public override bool RequestRunning(Guid guid)
		{
			lock (ApiLockSync)
			{
				if (_pendingProcessing.Select(asyncResult => (PrivateRequestData)asyncResult.AsyncState).Any(pending => pending.ApiGuidToken.Equals(guid)))
				{
					return true;
				}
			}

			return false;
		}

		public override bool CancelRequest(Guid guid)
		{
			return false;
		}

		public override Guid InitiateGet(string url, WebServiceResult result, IDictionary<string, string> parameters, object userState)
		{
			var data = CreateGetRequestData(url, result, parameters, userState);

			lock (ApiLockSync)
			{
				_queuedRequest.Enqueue(data);
				Monitor.Pulse(ApiLockSync);
			}

			return data.ApiGuidToken;
		}

		public override Guid InitiatePost(string url, WebServiceResult result, string writableString, object userState)
		{
			var data = CreatePostRequestDataFromString(url, result, writableString, userState);

			lock (ApiLockSync)
			{
				_queuedRequest.Enqueue(data);
				Monitor.Pulse(ApiLockSync);
			}

			return data.ApiGuidToken;
		}

		#endregion
	}
}