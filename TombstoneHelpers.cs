using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using Where.Common.Diagnostics;

namespace Where
{
	/// <summary>
	/// LAZYYYYYY :))))))))))
	/// </summary>
	public static class TombstoneHelpers
	{
		static TombstoneHelpers()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
														  {
															  PhoneApplicationService.Current.Deactivated += StoreStuffToAppropriateStates;
														  });
		}

		public static void Start()
		{
			return;
		}

		private static void StoreStuffToAppropriateStates(object sender, EventArgs e)
		{
			if (AppState.GetCurrentAppState == CurrentAppState.Exiting)
				return;

			foreach (var tombstoneDataClass in SavedObjects)
			{
				var data = tombstoneDataClass.Value;
				if (data.TargetStateObjectDictionary != null)
					try
					{
						var jsonString = JsonConvert.SerializeObject(data.TombstoneableObject);
						PhoneApplicationService.Current.State.Add(tombstoneDataClass.Key, jsonString);
					}
					catch (Exception x)
					{
						Debug.WriteLine("Could not deserialize object and save to state: {0}", x.Message);
					}
			}
		}


		private static readonly IDictionary<string, TombstoneDataClass> SavedObjects = new Dictionary<string, TombstoneDataClass>();

		private enum TombstoneTarget
		{
			NoState = 0,
			ApplicationState,
			PageState,
		}

		private class TombstoneDataClass
		{
			private readonly TombstoneTarget _tombstoneTarget;
			private readonly string _className;
			private readonly IDictionary<string, object> _targetStateObjectDictionary;

			public TombstoneDataClass(TombstoneTarget tombstoneTarget, object tombstoneableObject, string className, IDictionary<string, object> targetStateObjectDictionary)
			{
				_tombstoneTarget = tombstoneTarget;
				_targetStateObjectDictionary = targetStateObjectDictionary;
				_className = className;
				TombstoneableObject = tombstoneableObject;
			}

			public IDictionary<string, object> TargetStateObjectDictionary
			{
				get { return _targetStateObjectDictionary; }
			}

			public string ClassName
			{
				get { return _className; }
			}

			public object TombstoneableObject { get; set; }

			public TombstoneTarget GetTombstoneTarget
			{
				get { return _tombstoneTarget; }
			}

			private static readonly TombstoneDataClass DefaultValue = new TombstoneDataClass(TombstoneTarget.NoState, null, "EmptyType", null);

			/// <summary>
			/// Get a default class with no state
			/// </summary>
			/// <returns></returns>
			public static TombstoneDataClass Default()
			{
				return DefaultValue;
			}

			private string _stringCache;
			public override string ToString()
			{
				if (string.IsNullOrEmpty(_stringCache))
				{
					_stringCache = string.Format("Value Type:{0}, Object ToString:{1}", _className ?? "No Type", TombstoneableObject ?? "No Data");
				}
				return _stringCache;
			}
		}


		private static TombstoneDataClass GetDataClassForKey(string key)
		{
			if (SavedObjects.ContainsKey(key))
				return SavedObjects[key];

			return TombstoneDataClass.Default();
		}

		/// <summary>
		/// Save an object to application state by Key. If you try to resave for a same key, it will overwrite the previous one.
		/// </summary>
		/// <param name="page">Page making the save</param>
		/// <param name="pageKey">Key to save</param>
		/// <param name="value">Value of the object</param>
		public static void SaveObjectToApplicationState(this PhoneApplicationPage page, string pageKey, object value)
		{
			var key = GenerateKeyFromPageAndKey(page, pageKey);
			var getDataClass = GetDataClassForKey(key);

			if (getDataClass == TombstoneDataClass.Default())
			{
				var newDataClass = new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, PhoneApplicationService.Current.State);
				SavedObjects.Add(key, newDataClass);
			}
			else
			{
				if (getDataClass.ClassName == value.GetType().Name)
				{
					getDataClass.TombstoneableObject = value;
					WhereDebug.WriteLine(string.Format("Changing state value for key \"{0}\"...", key));
				}
				else
				{
					WhereDebug.WriteLine(string.Format("Changing state value and state type for key: \"{0}\"", key));
					SavedObjects.Remove(key);
					SavedObjects.Add(key, new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, PhoneApplicationService.Current.State));
				}

			}
		}

		/// <summary>
		/// Remove object from application state
		/// </summary>
		/// <param name="page"></param>
		/// <param name="userKey"></param>
		public static void RemoveObjectFromApplicationState(this PhoneApplicationPage page, string userKey)
		{
			var key = GenerateKeyFromPageAndKey(page, userKey);
			var dataClass = GetDataClassForKey(key);

			if (dataClass.GetTombstoneTarget == TombstoneTarget.NoState)
			{
				WhereDebug.WriteLine(string.Format("Key {0} doesn't exist!", userKey));

			}
			else if (PhoneApplicationService.Current.State.DictionaryContainsValueSafe(key))
			{
				PhoneApplicationService.Current.State.Remove(key);
			}

			SavedObjects.Remove(key);
		}

		/// <summary>
		/// Remove all keys from a page
		/// </summary>
		/// <param name="page"></param>
		public static void RemoveAllKeysFromPage(this PhoneApplicationPage page)
		{
			var pageName = page.GetType().Name;
			var pageNamePrefix = string.Format("{0}_", pageName);

			Debug.WriteLine("Cleaning up keys for page \"{0}\"", page.GetType().Name);

			var list = SavedObjects.Where(tombstoneDataClass => tombstoneDataClass.Key.StartsWith(pageNamePrefix)).ToList();
			foreach (var tombstoneDataClass in list)
			{
				SavedObjects.Remove(tombstoneDataClass);
			}

			var savedList = PhoneApplicationService.Current.State.Where(item => item.Key.StartsWith(pageNamePrefix)).ToList();
			foreach (var keyValuePair in savedList)
			{
				PhoneApplicationService.Current.State.Remove(keyValuePair);
			}
		}

		public static bool ContainsStateElementsForPage(this PhoneApplicationPage page)
		{
			var pageNameWithPrefix = page.GetType().Name + "_";
			var hasAny = SavedObjects.Any(value => value.Key.StartsWith(pageNameWithPrefix));
			var hasAnyState = PhoneApplicationService.Current.State.Any(kvp => kvp.Key.StartsWith(pageNameWithPrefix));

			return hasAny || hasAnyState;
		}

		public static T RestoreObjectFromApplicationState<T>(this PhoneApplicationPage page, string pageKey, T defaultValue = default(T))
		{
			var key = GenerateKeyFromPageAndKey(page, pageKey);
			var getDataClass = GetDataClassForKey(key);

			T output;

			if (getDataClass.GetTombstoneTarget != TombstoneTarget.NoState)
			{
				if (getDataClass.TombstoneableObject.GetType() == typeof(T))
					output = (T)getDataClass.TombstoneableObject;
				else
					throw new NotSupportedException("This is not of a desired type!");
			}
			else
			{
				output = RestoreFromStateDirectly<T>(key);
				SaveObjectToApplicationState(page, pageKey, output);
			}

			return output;

		}

		/// <summary>
		/// Generate a page key for a user state key.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		private static string GenerateKeyFromPageAndKey(PhoneApplicationPage page, string key)
		{
			var name = page.GetType().Name;
			return string.Format("{0}_{1}", name, key);

		}

		public static bool StateExists(this PhoneApplicationPage page, string key)
		{
			var getDataClass = GetDataClassForKey(key);

			bool result;

			if (getDataClass != TombstoneDataClass.Default())
			{
				result = true;
			}
			else if (PhoneApplicationService.Current.State.ContainsKey(key))
			{
				result = true;
			}
			else
				result = false;

			return result;

		}

		private static T RestoreFromStateDirectly<T>(string key)
		{
			var dict = PhoneApplicationService.Current.State;
			T result;

			if (dict.ContainsKey(key))
			{
				var getObject = dict[key];

				if (!(getObject is string))
					result = default(T);
				else
					try
					{

						var jsonObject = JsonConvert.DeserializeObject<T>(getObject as string);
						result = jsonObject;
					}
					catch (Exception)
					{
						result = default(T);
					}
			}
			else
			{
				result = default(T);
			}

			return result;
		}

	}
}
;