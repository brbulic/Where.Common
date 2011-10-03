using System;
using System.Text;

namespace Where.Common.Services.Interfaces
{
	/// <summary>
	/// Interace for Generic Object queue. 
	/// </summary>
	/// <typeparam name="T">Basic operation data</typeparam>
	public interface IThreadObjectQueue<T> where T : class, IQueueOperationData
	{
		/// <summary>
		/// Send some operation data to the Thread Queue
		/// </summary>
		/// <param name="action">Method to exectute, returns TResult</param>
		/// <param name="onResult">TResult user object and guid</param>
		/// <param name="operationData">User object with data</param>
		/// <returns>Operation UUID</returns>
		Guid EnqueueOperation<TResult>(Func<T, TResult> action, Action<Guid, QueueResult<TResult>> onResult, T operationData);

		Guid QueueOnTop<TResult>(Func<T, TResult> action, Action<Guid, QueueResult<TResult>> onResult, T operationData);

		bool OperationPending(Guid guid);
		bool OperationPending(Predicate<T> byKey);

		bool OperationRunning(Guid guid);
		bool OperationRunning(Predicate<T> byKey);

		bool CancelOperation(Guid operationData);
		bool CancelOperation(Predicate<T> byKey);

		void Stop();

	}

	public interface IQueueOperationData
	{
		object TransferObject { get; }
	}
}
