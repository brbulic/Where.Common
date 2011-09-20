using System.ComponentModel;

namespace Where.Common.Logger
{
    /// <summary>
    /// Logging helper class to get time stamp information during the app startup process
    /// </summary>
    public class ErrorLog : INotifyPropertyChanged
    {

        #region Bindable Properties

        /// <summary>
        /// log event
        /// </summary>
        private string _logEvent;


        /// <summary>
        /// Gets or sets the log event description
        /// </summary>
        public string LogEvent
        {
            get { return _logEvent; }

            set
            {
                _logEvent = value;
                OnPropertyChanged("LogEvent");
            }
        }

        /// <summary>
        /// Gets or sets the log event message
        /// </summary>
        private string _message;


        /// <summary>
        /// Gets or sets the log event message
        /// </summary>
        public string Message
        {
            get { return _message; }

            set
            {
                _message = value;
                OnPropertyChanged("Message");
            }
        }


        #endregion Bindable properties

        #region INotifyPropertyChanged

        /// <summary>
        /// Standard pattern for data binding and notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify subscribers of a change in the property
        /// </summary>
        /// <param name="propertyName">Name of the property to signal there has been a changed</param>
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            var args = new PropertyChangedEventArgs(propertyName);
            PropertyChanged(this, args);
        }

        #endregion


        /// <summary>
        /// Set the description and a null message
        /// </summary>
        /// <param name="logEvent">The log event to log</param>
        public ErrorLog(string logEvent)
        {
            LogEvent = logEvent;
            Message = "";
        }

        /// <summary>
        /// Set the description and error message
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        /// <param name="message">The time to log for the event.</param>
        public ErrorLog(string logEvent, string message)
        {
            LogEvent = logEvent;
            Message = message;
        }
    }
}
