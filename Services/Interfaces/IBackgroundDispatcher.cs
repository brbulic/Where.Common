using System;

namespace Where.Common.Services.Interfaces
{
    /// <summary>
    /// Operation callback
    /// </summary>
    /// <typeparam name="T">Type safe reference of the return data</typeparam>
    /// <param name="data">Reference of type T as a parameter on operation completed</param>
    public delegate void OperationCallback<T>(T data);

    /// <summary>
    /// Dispatcher that executes delegates on the background thread.
    /// </summary>
    public interface IBackgroundDispatcher
    {
        /// <summary>
        /// Dispatches an full, type safe, parametered operation with a result to the background thread with an optional callback.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operation">Input function with a result</param>
        /// <param name="backgroundOperationData">Data for the operation</param>
        /// <param name="callback">Calls on completed</param> 
        void QueueOperation<TResult>(Func<IBackgroundOperationData, TResult> operation, IBackgroundOperationData backgroundOperationData, OperationCallback<TResult> callback = null) where TResult : class;

        /// <summary>
        /// Do a simple, parameterless action on another thread.
        /// </summary>
        /// <param name="operation">Method (delegate) to exectute</param>
        /// <param name="callback">When completed</param>
        void QueueSimple(Action operation, Action callback = null);


        /// <summary>
        /// Do an action with a user state object on another thread.
        /// </summary>
        /// <param name="operation">Method (delegate) to exectute</param>
        /// <param name="userState">User data to use for excecution parameter</param>
        /// <param name="callback">Call on operation completed!</param>
        void QueueSimple(Action<object> operation, object userState, Action callback = null);


    }

}
