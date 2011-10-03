using System.Diagnostics;
using Where.Common.Services.Interfaces;

namespace Where.Common.Services
{
    /// <summary>
    /// Abstract class used to create ThreadListenerServices
    /// </summary>
    public abstract class WhereService<T> where T : class
    {
        protected bool UseCallback { get; set; }

        /// <summary>
        /// Dispatch an Operation to the BackgroundDispatcher
        /// </summary>
        /// <param name="data"></param>
        protected void DispatchOperation(IBackgroundOperationData data)
        {
            OperationCallback<T> op = null;
            if (UseCallback)
                op = ServiceCallback;

            Utils.BackgroundWorkerDefault.QueueOperation(QueuedOperation, data, op);
        }

        protected abstract T QueuedOperation(IBackgroundOperationData backgroundOperationData);

        protected virtual void ServiceCallback(T returnedData)
        {
            if (returnedData == null)
                Debug.WriteLine("Data is null... no data baby :)");
        }

        #region Publics

        public abstract void EnqueueOperation();

        #endregion

    }

}
