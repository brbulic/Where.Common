using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	/// <summary>
	/// Implemented instance that runs on a background thread.
	/// </summary>
	internal class BackgroundDispatcher : IBackgroundDispatcher
	{
		private readonly string _operationNullDataString = Guid.NewGuid().ToString();

		private readonly Thread _workerThread;

		private readonly object _queueLock = new object();

		private volatile bool _isRunning;

		private readonly Queue<InternalMessagePassing> _operationsQueue = new Queue<InternalMessagePassing>();

		internal BackgroundDispatcher()
		{
			_workerThread = new Thread(ThreadRunnableMethod) { IsBackground = true };
			_isRunning = true;
			_workerThread.Start();
			Deployment.Current.Dispatcher.BeginInvoke(() => Application.Current.Exit += ApplicationExitHandler);
		}

		private void ApplicationExitHandler(object sender, EventArgs e)
		{
			_isRunning = false;

			lock (_queueLock)
			{
				Monitor.Pulse(_queueLock);
			}
		}

		private void ThreadRunnableMethod()
		{
			Thread.CurrentThread.Name = "BackgroundDispatcherWorkerThread";

			var localQueue = new Queue<InternalMessagePassing>();

			while (_isRunning)
			{
				lock (_queueLock)
				{
					if (_operationsQueue.Count == 0)
						Monitor.Wait(_queueLock);

					if (!_isRunning)
						continue;

					while (_operationsQueue.Count > 0)
					{
						localQueue.Enqueue(_operationsQueue.Dequeue());
					}
				}

				while (localQueue.Count > 0)
				{
					if (!_isRunning)
						continue;

					var operationsData = localQueue.Dequeue();

					// Yield CPU time before operation
					Thread.Sleep(0);

					var result = operationsData.Delegate(operationsData.Data);

					// Yield CPU time after operation
					Thread.Sleep(0);

					if (operationsData.Callback != null)
						operationsData.Callback(result);


					// Yield CPU time before next cycle
					Thread.Sleep(0);
				}
			}
		}


		#region Public Methods

		/// <summary>
		/// Dispatches an operation to the background thread.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="operation">Input function with a result</param>
		/// <param name="operationData">Data for the operation</param>
		/// <param name="callback">Calls on completed</param>
		public void QueueOperation<T>(Func<IOperationData, T> operation, IOperationData operationData, OperationCallback<T> callback = null) where T : class
		{
			if (!_isRunning)
			{
				Debug.WriteLine("Application Exiting: Ignoring all new operations...");
				return;
			}

			lock (_queueLock)
			{
				Func<IOperationData, object> convertedFunction = opData =>
				{
					var result = operation(opData);
					return (object)result;
				};

				Action<object> convertedCallback = null;

				if (callback != null)
					convertedCallback = ConvertCallback(callback);

				_operationsQueue.Enqueue(new InternalMessagePassing(convertedFunction, operationData, convertedCallback, typeof(T)));
				Monitor.Pulse(_queueLock);
			}
		}

		public void QueueSimple(Action operation, Action callback = null)
		{
			if (!_isRunning)
			{
				Debug.WriteLine("Application Exiting: Ignoring all new operations...");
				return;
			}

			lock (_queueLock)
			{

				Func<IOperationData, object> convertedFunction = opdata =>
																	 {
																		 operation();
																		 return _operationNullDataString;
																	 };
				Action<object> convertedCallback = null;
				if (callback != null)
				{
					OperationCallback<object> obj = state => callback();
					convertedCallback = ConvertCallback(obj);
				}

				_operationsQueue.Enqueue(new InternalMessagePassing(convertedFunction, null, convertedCallback, null));

				Monitor.Pulse(_queueLock);
			}
		}

		public void QueueSimple(Action<object> operation, object userState, Action callback)
		{
			if (!_isRunning)
			{
				Debug.WriteLine("Thread not running");
			}

			lock (_queueLock)
			{
				var data = new BackgroundOperationData(OperationPriority.Normal, userState);

				Func<IOperationData, object> convertedFunction = opData =>
																	 {
																		 operation(opData.UserState);
																		 return opData.UserState;
																	 };

				Action<object> convertedCallback = null;
				if (callback != null)
				{
					OperationCallback<object> obj = state => callback();
					convertedCallback = ConvertCallback(obj);
				}

				_operationsQueue.Enqueue(new InternalMessagePassing(convertedFunction, data, convertedCallback, null));

				Monitor.Pulse(_queueLock);
			}
		}


		private static Action<object> ConvertCallback<T>(OperationCallback<T> callback)
		{
			Action<object> convertedCallback = res => callback((T)res);

			return convertedCallback;
		}

		#endregion

		private struct InternalMessagePassing
		{
			private readonly IOperationData _data;
			private readonly Action<object> _callback;
			private readonly Func<IOperationData, object> _delegate;
			private readonly Type _returnType;
			private readonly bool _isSynchronous;

			public InternalMessagePassing(Func<IOperationData, object> @delegate, IOperationData data, Action<object> callback, Type returnType, bool isSynchronous = false)
				: this()
			{
				_data = data;
				_isSynchronous = isSynchronous;
				_returnType = returnType;
				_delegate = @delegate;
				_callback = callback;
			}

			public bool IsSynchronous
			{
				get { return _isSynchronous; }
			}

			public Type ReturnType
			{
				get { return _returnType; }
			}

			public Func<IOperationData, object> Delegate
			{
				get { return _delegate; }
			}

			public Action<object> Callback
			{
				get { return _callback; }
			}

			public IOperationData Data
			{
				get { return _data; }
			}
		}
	}

	/// <summary>
	/// Operation data that holds the objects/references needed to execute an operation
	/// </summary>
	public sealed class BackgroundOperationData : IOperationData
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

		public object UserState
		{
			get { return _userState; }
		}

		#endregion
	}

}
