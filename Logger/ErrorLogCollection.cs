using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Where.Common.Logger
{
    /// <summary>
    /// Diagnostic error collection class
    /// </summary>
    public class ErrorLogCollection : IEnumerable<ErrorLog>
    {
        private readonly IList<ErrorLog> _backingField = new ObservableCollection<ErrorLog>();

        private static ErrorLogCollection _instance;
        public static ErrorLogCollection Instance
        {
            get { return _instance ?? (_instance = new ErrorLogCollection()); }
        }

        private ErrorLogCollection()
        {
            Debug.WriteLine("CreatedDetaultInstance of {0}", GetType().Name);
        }

        /// <summary>
        /// Add an error using log event, log message
        /// </summary>
        /// <param name="logEvent">Log event name</param>
        /// <param name="logMessage">Message that appeared</param>
        public void AddError(string logEvent, string logMessage)
        {
            lock (Utils.UniversalThreadSafeAccessLockObject)
            {
                _backingField.Add(new ErrorLog(logEvent, logMessage));
            }
        }

        public int Count
        {
            get { return _backingField.Count; }

        }

        #region Implementation of IEnumerable

        public IEnumerator<ErrorLog> GetEnumerator()
        {
            return _backingField.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

}
