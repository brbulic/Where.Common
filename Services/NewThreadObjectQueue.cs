using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
	public class NewThreadObjectQueue<T> : IThreadObjectQueue<T> where T : class, IQueueOperationData
	{

		#region Privates

		private readonly HandleLocker<Queue<InternalProcessData>> _pendingExecutionContainer;

		private readonly HandleLocker<IList<Guid>> _pendingCancel;

		private InternalProcessData _runningRequest;

		private bool _cancelCurrentlyRunningRequest;

		private readonly object _lockObject = new object();

		private readonly Thread _newThread;

		private volatile bool _isRunning;

		private readonly int _timeout;

		#endregion

		public NewThreadObjectQueue(int opTimeout = 20)
		{
			_timeout = opTimeout;

			_pendingCancel = new HandleLocker<IList<Guid>>(new List<Guid>(), _lockObject);
			_pendingExecutionContainer = new HandleLocker<Queue<InternalProcessData>>(new Queue<InternalProcessData>(), _lockObject);
			_isRunning = true;
			_newThread = new Thread(ThreadOperation);
			Deployment.Current.Dispatcher.BeginInvoke(() => { Application.Current.Exit += OnApplicationExit; });
			_newThread.Start();
		}

		private void OnApplicationExit(object sender, EventArgs e)
		{
			lock (_lockObject)
			{
				_isRunning = false;
				_pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
				{
					queue.Clear();
					return true;
				});
				Monitor.Pulse(_lockObject);

			}
		}

		#region Implementation of IThreadObjectQueue<T>

		public Guid EnqueueOperation<TResult>(Func<T, TResult> action, Action<Guid, QueueResult<TResult>> onResult, T operationData)
		{
			var guid = Guid.NewGuid();

			// Generalization of the monads :)
			Func<T, object> requestConverter = value => (object)action(value);
			Action<Guid, QueueResult<object>> resultConverter = (g, o) => onResult(guid, new QueueResult<TResult>(operationData, (TResult)(o.Result)));
			// End of monad generalizations

			var data = new InternalProcessData(operationData, guid, requestConverter, resultConverter);
			_pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
																		{
																			queue.Enqueue(data);
																			Monitor.Pulse(_lockObject);
																			return true;
																		});
			return guid;
		}

		public Guid QueueOnTop<TResult>(Func<T, TResult> action, Action<Guid, QueueResult<TResult>> onResult, T operationData)
		{
			var guid = Guid.NewGuid();

			// Generalization of the monads :)
			Func<T, object> requestConverter = value => (object)action(value);
			Action<Guid, QueueResult<object>> resultConverter = (g, o) => onResult(guid, new QueueResult<TResult>(operationData, (TResult)(o.Result)));
			// End of monad generalizations

			var data = new InternalProcessData(operationData, guid, requestConverter, resultConverter);

			_pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
			                                                        	{
			                                                        		var tempQueue = new Queue<InternalProcessData>();
			                                                        		while (queue.Count != 0)
			                                                        		{
			                                                        			tempQueue.Enqueue(queue.Dequeue());
			                                                        		}

			                                                        		queue.Enqueue(data);
			                                                        		while (tempQueue.Count != 0)
			                                                        		{
			                                                        			queue.Enqueue(tempQueue.Dequeue());
			                                                        		}

			                                                        		return true;
			                                                        	});
			return guid;
		}

		public bool OperationPending(Guid guid)
		{
			return _pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
																			{
																				var result = queue.Any(element => element.OperationId == guid);
																				Monitor.Pulse(_lockObject);
																				return result;
																			});
		}

		public bool OperationPending(Predicate<T> byKey)
		{
			Func<InternalProcessData, bool> derivate = value => byKey(value.Data);
			return _pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
																			{
																				var result = queue.Any(derivate);
																				Monitor.Pulse(_lockObject);
																				return result;
																			}

				);
		}

		public bool OperationRunning(Guid guid)
		{
			bool result;

			lock (_lockObject)
			{
				result = _runningRequest != null && _runningRequest.OperationId.Equals(guid);
				Monitor.Pulse(_lockObject);
			}

			return result;
		}

		public bool OperationRunning(Predicate<T> byKey)
		{
			lock (_lockObject)
			{
				return _runningRequest != null && byKey(_runningRequest.Data);
			}
		}

		public bool CancelOperation(Guid operationData)
		{
			bool result;

			lock (_lockObject)
			{
				result = OperationRunning(operationData);
				if (result)
				{
					_cancelCurrentlyRunningRequest = true;
				}
				else
				{
					if (OperationPending(operationData))
					{
						result = _pendingCancel.ExecuteSafeOperationOnObject(data =>
																				{
																					if (!data.Contains(operationData))
																					{
																						data.Add(operationData);
																					}

																					return true;
																				});
					}
				}
			}

			return result;

		}

		public bool CancelOperation(Predicate<T> byKey)
		{
			bool result;

			lock (_lockObject)
			{
				result = OperationRunning(byKey);

				if (result)
				{
					_cancelCurrentlyRunningRequest = true;
				}
				Monitor.Pulse(_lockObject);
			}

			return result;
		}

		public void Stop()
		{
			lock (_lockObject)
			{
				_isRunning = false;
				Monitor.Pulse(_lockObject);
			}
		}

		#endregion

		private void ThreadOperation()
		{
			Thread.CurrentThread.Name = "ThreadObjectQueueNew_" + this.GetHashCode();

			var pendingLocal = new Queue<InternalProcessData>();

			while (_isRunning)
			{
				var empty = _pendingExecutionContainer.ExecuteSafeOperationOnObject(queue =>
																						{
																							if (queue.Count == 0)
																								return true;

																							pendingLocal.Enqueue(queue.Dequeue());
																							return false;
																						});
				if (empty)
					lock (_lockObject)
						Monitor.Wait(_lockObject);

				if (!_isRunning)
					break;

				while (pendingLocal.Count > 0)
				{
					var operation = pendingLocal.Dequeue();
					var contains = _pendingCancel.ExecuteSafeOperationOnObject(list => list.Contains(operation.OperationId));

					if (contains) // skip execution if should be cancelled
						continue;

					lock (_lockObject)
					{
						_cancelCurrentlyRunningRequest = false;
						_runningRequest = operation;
					}

					Thread.Sleep(0);

					var data = operation.Data;
					var result = operation.Action(data);

					lock (_lockObject)
					{
						_runningRequest = null;
					}

					if (!_cancelCurrentlyRunningRequest)
						operation.Result(operation.OperationId, new QueueResult<object>(operation.Data, result));

					Thread.Sleep(0);

					if (!_isRunning)
						break;
				}
			}

		}


		private sealed class InternalProcessData
		{

			private readonly Func<T, object> _action;
			private readonly Action<Guid, QueueResult<object>> _result;
			private readonly T _data;
			private readonly Guid _operationId;

			public InternalProcessData(T data, Guid operationId, Func<T, object> action, Action<Guid, QueueResult<object>> result)
			{
				_data = data;
				_result = result;
				_action = action;
				_operationId = operationId;
			}

			public Action<Guid, QueueResult<object>> Result
			{
				get { return _result; }
			}

			public Func<T, object> Action
			{
				get { return _action; }
			}

			public Guid OperationId
			{
				get { return _operationId; }
			}

			public T Data
			{
				get { return _data; }
			}
		}
	}
}