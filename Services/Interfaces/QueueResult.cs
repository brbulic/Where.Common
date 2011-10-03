using System;

namespace Where.Common.Services.Interfaces
{
	public sealed class QueueResult<TResult> : EventArgs
	{
		private readonly IQueueOperationData _request;
		private readonly TResult _result;

		public QueueResult(IQueueOperationData request, TResult result)
		{
			_request = request;
			_result = result;
		}

		public TResult Result
		{
			get { return _result; }
		}

		public IQueueOperationData Request
		{
			get { return _request; }
		}
	}
}