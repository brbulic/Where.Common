using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Phone.Info;
using Where.Common.Logger;
using Where.Common.Services;
using Where.Common.Services.Interfaces;

namespace Where
{
	public static partial class Utils
	{

		#region Consts

		internal static readonly object UniversalThreadSafeAccessLockObject = new object();

		private const string UsPhoneFormatPattern = @"(\d{3})(\d{3})(\d{4})";

		#endregion


		private static StringBuilder _builder;
		public static StringBuilder StringBuilder
		{
			get
			{
				lock (UniversalThreadSafeAccessLockObject)
				{

					return _builder ?? (_builder = new StringBuilder());
				}
			}
		}



		/// <summary>
		/// Gets the current ErrorLog collection
		/// </summary>
		public static ErrorLogCollection ErrorLogInstance
		{
			get { return ErrorLogCollection.Instance; }
		}

		private static string _cache;
		public static string GetDeviceId()
		{
			if (string.IsNullOrEmpty(_cache))
			{

				object uniqueId;
				if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out uniqueId))
				{
					var myString = ByteArrayToString((byte[])uniqueId);
					_cache = myString;
				}
				else
				{
					_cache = String.Empty;
				}
			}

			return _cache;
		}


		/// <summary>
		/// Gets a phone a formats it to look like a US phone number.
		/// </summary>
		/// <param name="num"></param>
		/// <returns></returns>
		public static string FormatUsPhone(string num)
		{
			if (string.IsNullOrEmpty(num))
				return "";

			num = num.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");

			var results = Regex.Replace(num, UsPhoneFormatPattern, "($1) $2-$3");
			return results;
		}

		private static string ByteArrayToString(byte[] bytes)
		{
			var hex = new StringBuilder(bytes.Length * 2);
			foreach (var oneByte in bytes)
				hex.AppendFormat("{0:x2}", oneByte);
			return hex.ToString();
		}

		private static string _whereUserId;
		/// <summary>
		/// Get Where User ID for the API operations
		/// </summary>
		public static string GetWhereUserId
		{
			get { return _whereUserId ?? (_whereUserId = string.Format("wp7{0}", GetDeviceId())); }
		}

#if DEBUG
		private static string _anonymousUserId;
		public static string GetAnonymousUserId
		{
			get
			{
				if (string.IsNullOrEmpty(_anonymousUserId))
				{
					object obj;
					var anid = UserExtendedProperties.TryGetValue("ANID", out obj);
					if (anid)
					{
						var anidStr = (string)obj;
						_anonymousUserId = anidStr.Substring(2, 32);
					}
					else
					{
						_anonymousUserId = "not available";
					}
				}

				return _anonymousUserId;
			}

		}

#endif

	}

}
