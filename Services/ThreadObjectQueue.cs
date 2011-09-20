using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Linq;

namespace Where.Common.Services
{
    /// <summary>
    /// Virtualizes an Operations QUEUE thread
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThreadObjectQueue<T>
    {
        private readonly Queue<T> _operationsQueue = new Queue<T>();

        private EventWaitHandle _currentEventHandle = new ManualResetEvent(false);

        private readonly IList<T> _toCancel = new List<T>();

        private readonly object _queueLock = new object();

        private readonly object _cancelLock = new object();

        private volatile bool _isRunning;

        private Thread _runnerThread;

        #region Public properties

        public int TimeoutInSeconds { get; set; }

        #endregion

        #region Executor functions

        private readonly Func<bool> _runnerPredicate;

        private readonly Action<T> _operationAction;

        private readonly Func<T> _processData;

        private readonly Action<T> _onFail;


        #endregion

        public ThreadObjectQueue(Action<T> operationAction, Action<T> onFail, Func<bool> runnerPredicate, Func<T> processData = null, int timeout = 20)
        {
            _operationAction = operationAction;
            _onFail = onFail;
            _processData = processData;
            _runnerPredicate = runnerPredicate;
            TimeoutInSeconds = timeout;
        }

        public void EnqueueOperation(T operation)
        {
            lock (_queueLock)
            {
                _operationsQueue.Enqueue(operation);
                Thread.Sleep(0);
                Monitor.Pulse(_queueLock);
            }
        }

        public bool OperationPending(T operation)
        {
            lock (_queueLock)
            {
                return _operationsQueue.Contains(operation);
            }
        }

        public bool OperationPending(Func<T, bool> byKey)
        {
            lock (_queueLock)
            {
                return _operationsQueue.Any(byKey);
            }
        }

        public bool CancelOperation(T operation)
        {
            lock (_cancelLock)
            {
                _toCancel.Add(operation);
                return true;
            }

        }

        public bool CancelOperation(Func<T, bool> byKey)
        {
            IList<T> gotEm;

            lock (_queueLock)
            {
                gotEm = _operationsQueue.Where(byKey).ToList();
            }

            if (gotEm.Count == 1)
            {
                return gotEm.Select(CancelOperation).FirstOrDefault();
            }

            if (gotEm.Count > 1)
            {
                throw new NotSupportedException();
            }

            return false;
        }

        public void StartOperation()
        {
            if (_isRunning) return;

            _isRunning = true;
            _runnerThread = new Thread(CommenceOperation) { IsBackground = true };
            _runnerThread.Name = string.Format("ThreadObjectQueue-{0}", _runnerThread.ManagedThreadId);
            _runnerThread.Start();
        }

        public void StopOperation()
        {
            if (_isRunning)
                lock (_queueLock)
                {
                    try
                    {
                        _currentEventHandle.Set();
                    }
                    catch (Exception k)
                    {
                        Debug.WriteLine("EventWaitHandle already disposed! {0}", k.Message);
                    }

                    Monitor.Pulse(_queueLock);
                    _isRunning = false;
                }
        }

        private void CommenceOperation()
        {
            var currentOperations = new Queue<T>();

            while (_isRunning)
            {
                lock (_queueLock)
                {
                    if (_operationsQueue.Count == 0)
                        Monitor.Wait(_queueLock);

                    if (!_isRunning)
                        break;

                    while (0 < _operationsQueue.Count)
                        currentOperations.Enqueue(_operationsQueue.Dequeue());
                }

                while (currentOperations.Count > 0)
                {
                    var currentOp = currentOperations.Dequeue();

                    lock (_cancelLock)
                    {
                        if (_toCancel.Contains(currentOp))
                        {
                            _toCancel.Remove(currentOp);
                            _onFail(currentOp);
                            continue;
                        }
                    }

                    bool result;

                    // Yield CPU time before operation
                    Thread.Sleep(0);

                    // create new handle, use disposable pattern
                    using (var current = _currentEventHandle = new ManualResetEvent(false))
                    {
                        // do operation with a new handle
                        _operationAction(currentOp);

                        // Yield CPU time after operation
                        Thread.Sleep(0);

                        // wait for operation to complete
                        result = current.WaitOne(TimeSpan.FromSeconds(15));

                        // Ensure crap doesn't get bottled up
                        current.Close();
                    }

                    if (!_isRunning)
                        break;

                    if (!result)
                    {
                        _onFail(currentOp);

                        // Yield CPU time before next cycle
                        Thread.Sleep(0);
                        continue;
                    }

                    if (_processData != null)
                        _processData();

                    GC.Collect();
                }

                if (!_runnerPredicate())
                    _isRunning = false;

                // Yield CPU time before next cycle
                Thread.Sleep(0);
            }
        }

        [SecurityCritical]
        public void SignalOperationDone()
        {
            try
            {
                _currentEventHandle.Set();
            }
            catch (Exception dis)
            {
                Debug.WriteLine("Ja sam uništen! {0}", dis.Message);
            }

        }
    }
}