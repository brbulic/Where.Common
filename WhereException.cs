using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Where.Common
{
	public class WhereException : Exception
	{
		private readonly string _why;
		public string Why { get { return _why; } }


		public WhereException(string message, string why)
			: base(message)
		{
			_why = why;
		}
	}

}
