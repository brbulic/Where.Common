using System;
using System.Collections.Generic;

namespace Where
{
	public static partial class Utils
	{
		private static readonly IDictionary<string, object> LocalDataBin = new Dictionary<string, object>();

		/// <summary>
		/// Adds an object to a Application wide Object dictionary to transfer data between pages.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="state"></param>
		public static void AddToDataBin(string key, object state)
		{
			if (LocalDataBin.ContainsKey(key))
			{
				LocalDataBin[key] = state;
			}
			else
			{
				LocalDataBin.Add(key, state);
			}

		}

		/// <summary>
		/// Pulls an object from an Application wide Object dictionary. Removes object on successful return (when the result isn't null or default(T)).
		/// </summary>
		/// <typeparam name="T">The parameter in the object state</typeparam>
		/// <param name="key">Dictionary key of the object</param>
		/// <param name="defaultValue">Uses default(T) as default, but if set, returns that value if the key doesn't exist. </param>
		/// <returns>The object casted to a type T if success</returns>
		public static T PullFromDataBin<T>(string key, T defaultValue = default(T))
		{
			T result;

			if (LocalDataBin.ContainsKey(key))
			{
				try
				{
					result = (T)LocalDataBin[key];
					LocalDataBin.Remove(key);
				}
				catch (Exception e)
				{
					result = defaultValue;
					ErrorLogInstance.AddError("PullFromDataBin", e.Message);
				}
			}
			else
			{
				result = defaultValue;
			}

			return result;
		}

	}
}
