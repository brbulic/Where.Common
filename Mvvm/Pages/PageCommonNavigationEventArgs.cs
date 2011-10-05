using System;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace Where.Common.Mvvm
{

	public class PageCommonEventArgs : EventArgs
	{
		private readonly string _pageName;

		public PageCommonEventArgs()
		{
			_pageName = String.Empty;
		}

		public PageCommonEventArgs(string name)
		{
			_pageName = name;
		}

		public string PageName
		{
			get { return _pageName; }
		}
	}

	public class PageCommonNavigationEventArgs : PageCommonEventArgs
	{
		private readonly bool _isPageLoaded;
		private readonly IDictionary<string, string> _parsedQueryStrings;
		private readonly bool _hasBeenTombstoned;
		private readonly Uri _localUri;
		private readonly object _content;

		public PageCommonNavigationEventArgs(bool isPageLoaded, IDictionary<string, string> parsedQueryStrings, bool hasBeenTombstoned, object content, Uri uri)
		{
			_isPageLoaded = isPageLoaded;
			_hasBeenTombstoned = hasBeenTombstoned;
			_parsedQueryStrings = parsedQueryStrings;
			_content = content;
			_localUri = uri;
		}

		public object Content
		{
			get { return _content; }
		}

		public Uri LocalUri
		{
			get { return _localUri; }
		}

		public bool HasBeenTombstoned
		{
			get { return _hasBeenTombstoned; }
		}

		public IDictionary<string, string> ParsedQueryStrings
		{
			get { return _parsedQueryStrings; }
		}

		public bool IsPageLoaded
		{
			get { return _isPageLoaded; }
		}
	}
};