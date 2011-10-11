using System;

namespace Where.Common
{
	public class WhereException : Exception
	{
		private readonly string _why;
		public string Why { get { return _why; } }


		public WhereException(string message, string why)
			: base(string.Format("{0}! Beacuse of: {1}", message, why))
		{
			_why = why;
		}
	}

}
