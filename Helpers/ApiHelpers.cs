﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Where
{
	public partial class Utils
	{


		#region Consts

		const string UrlPattern = @"^(http|https)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*$";

		#endregion

		/// <summary>
		/// Convert an Dictionary of HTTP GET parameters to a URL string. Begins with "?"
		/// IMPORTANT: it doesn't do URL encoding for values.
		/// </summary>
		/// <param name="arguments">Dictionary of arguments</param>
		/// <param name="ranval">If true, appends the random value to parameters to bypass Browser caching</param>
		/// <param name="builder"></param>
		/// <returns>The string containing all parameters.</returns>
		public static string ProcessArguments(IDictionary<string, string> arguments, bool ranval = true, StringBuilder builder = null)
		{

			string returner;

			if (builder == null)
			{
				// Aquire builder 
				var builderHandle = GetStringBuilderWithHandle;
				returner = builderHandle.ExecuteSafeOperationOnObject(str => RealProcessArguments(arguments, str, ranval));
			}
			else
			{
				returner = RealProcessArguments(arguments, builder, ranval);
			}

			return returner;
		}

		private static string RealProcessArguments(ICollection<KeyValuePair<string, string>> args, StringBuilder builder, bool ranval)
		{
			string result;

			// Prepare builder
			builder.Flush();
			builder.Append("?");

			if (args == null || args.Count == 0 && ranval)
			{
				builder.Append("ranval=").Append(Environment.TickCount);
				result = builder.ToString();
			}
			else
			{
				foreach (var keyValuePair in args)
					builder.Append(keyValuePair.Key).Append("=").Append(keyValuePair.Value).Append("&");
				if (ranval)
					builder.Append("ranval=").Append(Environment.TickCount);
				else
					builder.Remove(builder.Length - 1, 1);

				result = builder.ToString();
			}

			return result;
		}


		/// <summary>
		/// URL encode a string
		/// </summary>
		/// <param name="toEncode"></param>
		/// <returns></returns>
		public static string UrlEncode(this string toEncode)
		{
			return System.Net.HttpUtility.UrlEncode(toEncode);
		}

		/// <summary>
		/// Checks if a certain string is a WEB URL
		/// </summary>
		/// <param name="httpLink">Candidate URL</param>
		/// <returns>True if it IS a WEB URL</returns>
		public static bool IsWebPage(string httpLink)
		{
			Debug.Assert(!string.IsNullOrEmpty(httpLink));

			return Regex.IsMatch(httpLink, UrlPattern, RegexOptions.IgnoreCase);
		}

	}
}
