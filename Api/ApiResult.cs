using System;

namespace Where.Api
{
	/// <summary>
	/// Standard ApiResult containing string and exception.
	/// </summary>
	public sealed class ApiResult
	{
		private readonly Object _userState;
		private readonly Exception _error;
		private readonly String _responseString;
		private readonly WebApiServerStatus _resultStatus;

		private ApiResult()
		{
			_userState = null;
			_error = new Exception();
			_responseString = String.Empty;
		}

		/// <summary>
		/// What's the situation here?
		/// </summary>
		public WebApiServerStatus ResultStatus
		{
			get { return _resultStatus; }
		}

		/// <summary>
		/// The string recieved by a HTTP request
		/// </summary>
		public string ResponseString
		{
			get { return _responseString; }
		}

		/// <summary>
		/// If error, the exception error is shown here.
		/// </summary>
		public Exception Error
		{
			get { return _error; }
		}

		/// <summary>
		/// The user object reference used in the request is found here.
		/// </summary>
		public object UserState
		{
			get { return _userState; }
		}
		
		/// <summary>
		/// Create an ApiResult object for a completed request.
		/// </summary>
		/// <param name="userState">User state.</param>
		/// <param name="exception">Null if all OK.</param>
		/// <param name="result">Status.</param>
		/// <param name="responseString">Returned string from the request.</param>
		internal ApiResult(object userState, Exception exception, WebApiServerStatus result, string responseString)
		{
			_userState = userState;
			_responseString = responseString;
			_error = exception;
			_resultStatus = result;
		}

		private static ApiResult _empty;

		public static ApiResult CreateEmpty
		{
			get { return _empty ?? (_empty = new ApiResult()); }
		}


	}
}
