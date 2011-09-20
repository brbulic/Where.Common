using System;
using System.Net;

namespace Where.Api
{
    public sealed class ApiResult
    {
        private readonly Object _userState;
        private readonly Exception _error;
        private readonly bool _cancelled;
        private readonly String _result;


        private ApiResult()
        {
            _userState = null;
            _error = new Exception();
            _cancelled = false;
            _result = String.Empty;
        }

        public ApiResult(object userState, Exception error, bool cancelled, string result)
        {
            _userState = userState;
            _result = result;
            _cancelled = cancelled;
            _error = error;
        }

        public string Result
        {
            get { return _result; }
        }

        public bool Cancelled
        {
            get { return _cancelled; }
        }

        public Exception Error
        {
            get { return _error; }
        }

        public object UserState
        {
            get { return _userState; }
        }

        public static ApiResult FromDownloadStringCompletedEventArgs(DownloadStringCompletedEventArgs e)
        {
            ApiResult tempResult;

            try
            {
                tempResult = new ApiResult(e.UserState, e.Error, e.Cancelled, e.Result);

            }
            catch (Exception x)
            {
                Utils.ErrorLogInstance.AddError("FromDownloadStringCompletedEventArgs", x.Message);
                tempResult = new ApiResult(e.UserState, e.Error, e.Cancelled, String.Empty);
            }

            return tempResult;
        }

        private static ApiResult _empty;
        public static ApiResult CreateEmpty
        {
            get { return _empty ?? (_empty = new ApiResult()); }
        }


    }
}
