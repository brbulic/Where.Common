using System;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	/// <summary>
	/// Implemented instance that runs on a background thread.
	/// </summary>
	internal class BackgroundDispatcher : IBackgroundDispatcher
	{
		private readonly IThreadObjectQueue<IBackgroundOperationData> _objectQueue;

		internal BackgroundDispatcher(IThreadObjectQueue<IBackgroundOperationData> threadQueueInject)
		{
			_objectQueue = threadQueueInject;
		}

		#region Public Methods

		/// <summary>
		/// Dispatches an operation to the background thread.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="operation">Input function with a result</param>
		/// <param name="backgroundOperationData">Data for the operation</param>
		/// <param name="callback">Calls on completed</param>
		public void QueueOperation<TResult>(Func<IBackgroundOperationData, TResult> operation, IBackgroundOperationData backgroundOperationData, OperationCallback<TResult> callback = null) where TResult : class
		{

			Action<Guid, QueueResult<TResult>> resultFunc = (g, r) =>
																{
																	// It's so called "boxing"
																	var final = callback;
																	if (final != null)
																		final(r.Result);
																};


			_objectQueue.QueueOnTop(operation, resultFunc, backgroundOperationData);
		}

		public void QueueSimple(Action operation, Action callback = null)
		{
			Func<IBackgroundOperationData, object> func = op =>
															{
																operation();
																return null;
															};

			Action<Guid, QueueResult<object>> resultFunc = (g, qr) =>
															{
																var final = callback;
																if (final != null)
																	final();
															};

			_objectQueue.QueueOnTop(func, resultFunc, null);
		}

		public void QueueSimple(Action<object> operation, object userState, Action callback = null)
		{

			Func<IBackgroundOperationData, object> func = op =>
			{
				operation(op.TransferObject);
				return null;
			};

			Action<Guid, QueueResult<object>> resultFunc = (g, qr) =>
			{
				var final = callback;
				if (final != null)
					final();
			};


			_objectQueue.EnqueueOperation(func, resultFunc, new BackgroundOperationData(OperationPriority.Normal, userState));
		}


		private static Action<object> ConvertCallback<T>(OperationCallback<T> callback)
		{
			Action<object> convertedCallback = res => callback((T)res);

			return convertedCallback;
		}

		#endregion


		/// <summary>
		/// Operation data that holds the objects/references needed to execute an operation
		/// </summary>
		private sealed class BackgroundOperationData : IBackgroundOperationData
		{

			private readonly OperationPriority _priority;

			private readonly object _userState;


			public BackgroundOperationData(OperationPriority priority, object userState)
			{
				_priority = priority;
				_userState = userState;
			}

			#region Implementation of IOperationData


			public OperationPriority Priority
			{
				get { return _priority; }
			}

			#endregion

			#region Implementation of IQueueOperationData

			public object TransferObject
			{
				get { return _userState; }

			}

			#endregion
		}
	}



}
