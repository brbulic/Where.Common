using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Where
{
	public static partial class Utils
	{

		/// <summary>
		/// Cleans the String Builder
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static StringBuilder Flush(this StringBuilder builder)
		{
			var b = builder.Remove(0, builder.Length);
			GC.Collect();
			return b;
		}

		/// <summary>
		/// Extender method attached on INotifyPropertyChanged interfaces that handles the commonly used OnPropertyChanged event Invoker.
		/// </summary>
		/// <param name="eventOwner">Class holding the event</param>
		/// <param name="eventHandler">The Event itself!</param>
		/// <param name="propertyName">Property name that has changed!</param>
		public static void OnPropertyChanged(this INotifyPropertyChanged eventOwner, PropertyChangedEventHandler eventHandler, string propertyName)
		{
			var currentEventHandler = eventHandler;
			var currentEventOwner = eventOwner;

			if (currentEventHandler != null)
			{
				currentEventHandler(eventHandler, new PropertyChangedEventArgs(propertyName));
			}
		}


		public static TE GetValueFromDictionary<TE>(this IDictionary<string, TE> currentDictionary, string key, TE defaultValue = default(TE))
		{
			TE tempReturn;
			if (currentDictionary.ContainsKey(key))
			{
				tempReturn = currentDictionary[key];
			}
			else
			{
				tempReturn = !EqualityComparer<TE>.Default.Equals(defaultValue, default(TE)) ? defaultValue : default(TE);
			}

			return tempReturn;
		}

		public static void SetValueInDictionary<TE>(this IDictionary<string, TE> currentDictionary, string key, TE value)
		{

			if (currentDictionary.ContainsKey(key))
			{
				currentDictionary[key] = value;
			}
			else
			{
				currentDictionary.Add(key, value);
			}

		}

		public static bool DictionaryContainsValue<TE>(this IDictionary<string, TE> currentDictionary, string key)
		{
			return currentDictionary.ContainsKey(key);
		}


	}


}
