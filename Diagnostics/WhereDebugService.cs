using System;
using System.Diagnostics;
using System.Threading;
using Where.Common.Services;
using Where.Common.Services.Interfaces;

namespace Where.Common.Diagnostics
{
	internal class WhereDebugService : WhereService<WhereDebugService.DebugWorkerData>
	{
		private readonly ApiDebug _apiDebug;

		private readonly ManualResetEvent _event;

		private static WhereDebugService _currentInstance;
		internal static WhereDebugService Instance
		{
			get { return _currentInstance ?? (_currentInstance = new WhereDebugService()); }
		}

		public bool EnableOnlineDebugging { get; private set; }

		private WhereDebugService()
		{
			_apiDebug = new ApiDebug(Callback);
			_event = new ManualResetEvent(false);
			UseCallback = false;
		}

		internal void BeginRemoteDebug()
		{
			EnableOnlineDebugging = true;
		}

		private class DebugOperationData : IBackgroundOperationData
		{
			private readonly OperationPriority _priority;
			private readonly object _obj;

			public DebugOperationData(OperationPriority priority, object obj)
			{
				_priority = priority;
				_obj = obj;
			}

			#region Implementation of IQueueOperationData

			public object TransferObject
			{
				get { return _obj; }
			}

			#endregion

			#region Implementation of IBackgroundOperationData

			public OperationPriority Priority
			{
				get { return _priority; }
			}

			#endregion
		}

		internal void SendDebugMessage(string user, string text)
		{
			var data = new DebugWorkerData(user, text);
			DispatchOperation(new DebugOperationData(OperationPriority.Normal, data));
		}

		internal sealed class DebugWorkerData
		{
			private readonly string _user;
			private readonly string _tekst;

			public DebugWorkerData(string user, string tekst)
			{
				_user = user;
				_tekst = tekst;
			}

			public string Tekst
			{
				get { return _tekst; }
			}

			public string User
			{
				get { return _user; }
			}
		}

		#region Overrides of WhereService

		protected override DebugWorkerData QueuedOperation(IBackgroundOperationData backgroundOperationData)
		{
			var currentWorkerData = (DebugWorkerData)backgroundOperationData.TransferObject;

			_event.Reset();
			_apiDebug.DebugAdd(currentWorkerData.User, currentWorkerData.Tekst);
			var result = _event.WaitOne(TimeSpan.FromSeconds(10));

			if (!result)
				Debug.WriteLine("Error sending data!");


			return currentWorkerData;
		}

		protected override void ServiceCallback(DebugWorkerData returnedData)
		{
			return;
		}

		public override void EnqueueOperation()
		{
			throw new NotImplementedException();
		}

		#endregion

		private void Callback(bool error, object o)
		{
			_event.Set();

			if (error)
				Debug.WriteLine("Error sending Debug Message");

		}
	}
}
