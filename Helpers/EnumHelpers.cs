using System;

namespace Where
{
	public static partial class Utils
	{

		/// <summary>
		/// Returns the name of the Enum
		/// </summary>
		/// <typeparam name="T">Enum type</typeparam>
		/// <param name="selectedEnum">Enum instance?</param>
		/// <returns>Enum element name</returns>
		public static String EnumToString<T>(this Enum selectedEnum)
		{
			return Enum.GetName(typeof(T), selectedEnum);
		}

		/// <summary>
		/// Returns the name of the Enum
		/// </summary>
		/// <param name="selectedEnum"></param>
		/// <returns></returns>
		public static String EnumToString(this Enum selectedEnum)
		{
			return Enum.GetName(selectedEnum.GetType(), selectedEnum);
		}

		/// <summary>
		/// Converts a string to its Enum by type T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="stringValue">Enum string</param>
		/// <param name="ignoreCase">true: ignores case</param>
		/// <returns>The enum with the given name string.</returns>
		public static T StringToEnum<T>(String stringValue, bool ignoreCase)
		{
			return (T)Enum.Parse(typeof(T), stringValue, ignoreCase);
		}
	}
}
