using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Where.Api;
using Where.Common.Services;

namespace Where.Common.Services.Interfaces
{

    /// <summary>
    /// Web service interface that initiates the GET and POST requests to the web
    /// </summary>
    public interface IWebService
    {

		/// <summary>
        /// Check if a request is running
        /// </summary>
        /// <param name="guid">Guid of your request</param>
        /// <returns>Notifies the caller that the request is running</returns>
        bool RequestRunning(Guid guid);

        /// <summary>
        /// Initiate a Request cancellation, will immediately call the WebServiceResult with an empty response
        /// </summary>
        /// <param name="guid">Guid of the request</param>
        /// <returns>Notifies the user if the request cancellation is under way</returns>
        bool CancelRequest(Guid guid);

        /// <summary>
        /// Initiate a GET request, returns result string in the WebServiceResult callback
        /// </summary>
        /// <param name="url">Get URL</param>
        /// <param name="result">Response delegate</param>
        /// <param name="parameters">Get parameters (if any)</param>
        /// <param name="userState">User state (if any) </param>
        /// <returns>Guid of the request</returns>
        Guid InitiateGet(string url, WebServiceResult result, IDictionary<string, string> parameters = null, object userState = null);

        /// <summary>
        /// Initiate a POST request with an string to upload, returns the result string in the WebServiceResult callback
        /// </summary>
        /// <param name="url">Get URL</param>
        /// <param name="result">Response delegate</param>
        /// <param name="writableString">String to upload</param>
        /// <param name="userState">User state (if any)</param>
        /// <returns>Guid of the Request</returns>
        Guid InitiatePost(string url, WebServiceResult result, string writableString, object userState = null);

        /// <summary>
        /// Initiate a POST request with parameters to upload, return the result string in the WebServiceResult callback
        /// </summary>
        /// <param name="url">Get URL</param>
        /// <param name="result">Response delegate</param>
        /// <param name="parameters">Parameters to upload</param>
        /// <param name="userState">User state object (if any)</param>
        /// <returns>Guid of the Request</returns>
        Guid InitiatePost(string url, WebServiceResult result, IDictionary<string, string> parameters, object userState = null);

    }
}

namespace Where.Api
{
    /// <summary>
    /// WebApiService Async status callback.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="args">WebApiServer service event args</param>
    public delegate void WebServiceResult(Guid guid, WebServiceEventArgs args);

    /// <summary>
    /// Response status
    /// </summary>
    public enum WebApiServerStatus
    {
        /// <summary>
        /// I don't know what happened (default)
        /// </summary>
        Unknown,

        /// <summary>
        /// Url is invalid
        /// </summary>
        InvalidUrl,

        /// <summary>
        /// Cannot read/write with Stream
        /// </summary>
        InvalidStream,

        /// <summary>
        /// General error
        /// </summary>
        Error = InvalidUrl | InvalidStream,

        /// <summary>
        /// Request cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// All good, man :)
        /// </summary>
        Success,

        /// <summary>
        /// Has not internet connectivity
        /// </summary>
        NoConnection,
    }

    /// <summary>
    /// Thread-safe, immutable struct that has the recieved data for the specified request.
    /// </summary>
    public sealed class WebServiceEventArgs : EventArgs
    {
        private readonly WebApiServerStatus _currentStatus;
        private readonly Exception _currentException;
        private readonly string _resultString;
        private readonly object _state;

        public WebApiServerStatus ResponseStatus { get { return _currentStatus; } }
        public Exception Exception { get { return _currentException; } }
        public string ResponseString { get { return _resultString; } }
        public object UserState { get { return _state; } }
        internal bool IsHandled { get; set; }


        /// <summary>
        /// Create default
        /// </summary>
        /// <returns></returns>
        internal static WebServiceEventArgs CreateDefault(object userState)
        {
            return new WebServiceEventArgs(WebApiServerStatus.Unknown, new Exception("Unknown state reached"), String.Empty, userState);
        }
        
        internal WebServiceEventArgs(WebApiServerStatus status, Exception exception, string response, object state)
        {
            _currentStatus = status;
            _currentException = exception;
            _resultString = response;
            _state = state;
        }
    }

}
