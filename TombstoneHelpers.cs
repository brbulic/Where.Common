using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;

namespace Where
{
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

		private struct TombstoneDataClass
		{
			private readonly TombstoneTarget _tombstoneTarget;
			private object _tombstoneableObject;
			private readonly string _className;
			private readonly IDictionary<string, object> _targetStateObjectDictionary;

			public TombstoneDataClass(TombstoneTarget tombstoneTarget, object tombstoneableObject, string className, IDictionary<string, object> targetStateObjectDictionary)
			{
				_tombstoneTarget = tombstoneTarget;
				_targetStateObjectDictionary = targetStateObjectDictionary;
				_className = className;
				_tombstoneableObject = tombstoneableObject;
			}

			public IDictionary<string, object> TargetStateObjectDictionary
			{
				get { return _targetStateObjectDictionary; }
			}

			public string ClassName
			{
				get { return _className; }
			}

			public object TombstoneableObject
			{
				get { return _tombstoneableObject; }
				set { _tombstoneableObject = value; }
			}

			public TombstoneTarget GetTombstoneTarget
			{
				get { return _tombstoneTarget; }
			}
		}


		private static TombstoneDataClass GetDataClassForKey(string key)
		{
			if (SavedObjects.ContainsKey(key))
				return SavedObjects[key];

			return default(TombstoneDataClass);
		}

		public static void SaveObjectToApplicationState(this PhoneApplicationPage page, string pageKey, object value)
		{
			var key = GenerateKeyFromPageAndKey(page, pageKey);
			var getDataClass = GetDataClassForKey(key);

			if (getDataClass.GetTombstoneTarget == TombstoneTarget.NoState)
			{
				var newDataClass = new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, PhoneApplicationService.Current.State);
				SavedObjects.Add(key, newDataClass);
			}
			else
			{
				if (getDataClass.ClassName == value.GetType().Name)
					getDataClass.TombstoneableObject = value;
				else
				{
					Debug.WriteLine("Changing state value for key: \"{0}\"", key);
					SavedObjects.Remove(key);
					SavedObjects.Add(key, new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, PhoneApplicationService.Current.State));
				}

			}
		}

		public static void RemoveAllKeysFromPage(this PhoneApplicationPage page)
		{
			var pageName = page.GetType().Name;
			var pageNamePrefix = string.Format("{0}_", pageName);

			Debug.WriteLine("Cleaning up keys on page cleanup!");

			var list = SavedObjects.Where(tombstoneDataClass => tombstoneDataClass.Key.StartsWith(pageNamePrefix)).ToList();

			foreach (var tombstoneDataClass in list)
			{
				SavedObjects.Remove(tombstoneDataClass);
			}
		}

		public static bool ContainsStateElementsForPage(this PhoneApplicationPage page)
		{
			var pageNameWithPrefix = page.GetType().Name + "_";
			var hasAny = SavedObjects.Any(value => value.Key.StartsWith(pageNameWithPrefix));
			return hasAny;
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
			}

			return output;

		}

		private static string GenerateKeyFromPageAndKey(PhoneApplicationPage page, string key)
		{
			var name = page.GetType().Name;
			return string.Format("{0}_{1}", name, key);

		}

		public static bool StateExists(this PhoneApplicationPage page, string key)
		{
			var getDataClass = GetDataClassForKey(key);

			bool result;

			if (getDataClass.GetTombstoneTarget != TombstoneTarget.NoState)
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