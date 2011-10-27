using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using Where.Common.Diagnostics;
using Where.Common.Mvvm;

namespace Where
{
	/// <summary>
	/// LAZYYYYYY :))))))))))
	/// </summary>
	public static class TombstoneHelpers
	{

		internal const string PageNameTombstoneKey = "CurrentPageNameTombstoneKey";

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

		private static bool _usingSecondaryDictionary;
		public static bool UsingCustomDictionary
		{
			get { return _usingSecondaryDictionary; }
		}


		#region Lazy init of CurrentStateDictionary

		private static IDictionary<string, object> _backingCurrentStateDictionary;
		public static IDictionary<string, object> CurrentStateDictionary
		{
			get
			{
				if (_backingCurrentStateDictionary == null)
				{
					_backingCurrentStateDictionary = PhoneApplicationService.Current.State;
					_usingSecondaryDictionary = false;
				}

				return _backingCurrentStateDictionary;
			}
			set
			{
				_usingSecondaryDictionary = value != PhoneApplicationService.Current.State;
				_backingCurrentStateDictionary = value;
			}
		}

		#endregion


		public static void UseCustomDictionary(IDictionary<string, object> persistanceDictionary)
		{
			CurrentStateDictionary = persistanceDictionary ?? PhoneApplicationService.Current.State;
		}

		internal static void StoreStuffToAppropriateStates(object sender, EventArgs e)
		{
			if (AppState.GetCurrentAppState == CurrentAppState.Exiting)
				return;

			var currentApplicationState = CurrentStateDictionary;

			foreach (var tombstoneDataClass in SavedObjects)
			{
				var data = tombstoneDataClass.Value;
				if (data.TargetStateObjectDictionary != null)
					try
					{
						if (currentApplicationState.ContainsKey(tombstoneDataClass.Key))
							currentApplicationState.Remove(tombstoneDataClass.Key);

						var jsonString = JsonConvert.SerializeObject(data.TombstoneableObject);
						currentApplicationState.Add(tombstoneDataClass.Key, jsonString);
					}
					catch (Exception x)
					{
						Debug.WriteLine("Could not deserialize object and save to state: {0}", x.Message);
					}
			}

			SavedObjects.Clear();
		}


		#region TombstoneHelpers TestableExtenstions

		internal static void ResetStates()
		{
			SavedObjects.Clear();
			CurrentStateDictionary.Clear();
		}


		#endregion



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

			Utils.StringNotNullOrEmpty(pageKey, "A valid tombstone key must exist!");
			Utils.NotNullArgument(page, "pageKey", "There must be a page to tombstone data to!");
			
			//Utils.NotNullArgument(value, "value", "There must be tombstone data to tombstone!");

			var key = GenerateKeyFromPageAndKey(page, pageKey);
			var getDataClass = GetDataClassForKey(key);

			if (getDataClass == TombstoneDataClass.Default())
			{
				var newDataClass = new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, CurrentStateDictionary);
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
					SavedObjects.Add(key, new TombstoneDataClass(TombstoneTarget.ApplicationState, value, value.GetType().Name, CurrentStateDictionary));
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
			Utils.StringNotNullOrEmpty(userKey, "A valid tombstone key must exist!");
			Utils.NotNullArgument(page, "There must be a page to tombstone data to!");

			var key = GenerateKeyFromPageAndKey(page, userKey);
			var dataClass = GetDataClassForKey(key);

			if (dataClass.GetTombstoneTarget == TombstoneTarget.NoState)
			{
				WhereDebug.WriteLine(string.Format("Key {0} doesn't exist!", userKey));

			}
			else if (CurrentStateDictionary.DictionaryContainsValue(key))
			{
				CurrentStateDictionary.Remove(key);
			}

			SavedObjects.Remove(key);
		}

		/// <summary>
		/// Remove all keys from a page
		/// </summary>
		/// <param name="page"></param>
		public static void RemoveAllKeysFromPage(this PhoneApplicationPage page)
		{
			var pageNamePrefix = GetPageName(page);

			Debug.WriteLine("Cleaning up keys for page \"{0}\"", pageNamePrefix);

			var list = SavedObjects.Where(tombstoneDataClass => tombstoneDataClass.Key.StartsWith(pageNamePrefix)).ToList();
			foreach (var tombstoneDataClass in list)
			{
				SavedObjects.Remove(tombstoneDataClass);
			}

			var savedList = CurrentStateDictionary.Where(item => item.Key.StartsWith(pageNamePrefix)).ToList();
			foreach (var keyValuePair in savedList)
			{
				CurrentStateDictionary.Remove(keyValuePair);
			}
		}

		public static T RestoreObjectFromApplicationState<T>(this PhoneApplicationPage page, string pageKey, T defaultValue = default(T))
		{

			Utils.NotNullArgument(page, "There must a page be exist to Restore data from!");
			Utils.StringNotNullOrEmpty(pageKey, "A tombstone key must exist for using it to restore data!");

			var key = GenerateKeyFromPageAndKey(page, pageKey);
			var getDataClass = GetDataClassForKey(key);

			T output;

			if (getDataClass != TombstoneDataClass.Default())
			{
				if (getDataClass.TombstoneableObject.GetType() == typeof(T))
					output = (T)getDataClass.TombstoneableObject;
				else
					throw new NotSupportedException("This is not of a desired type!");
			}
			else
			{
				var restoreResult = RestoreFromStateDirectly<T>(key);

				switch (restoreResult.StatusMessage)
				{
					case StateRestoreData<T>.ErrorMessage.NoError:
						SaveObjectToApplicationState(page, pageKey, restoreResult.StatusMessage);
						output = restoreResult.CurrentValue;
						break;
					case StateRestoreData<T>.ErrorMessage.NotFound:
						output = restoreResult.CurrentValue;
						break;
					case StateRestoreData<T>.ErrorMessage.WrongType:
						throw new NotSupportedException("This is not of a desired type!");
					default:
						throw new ArgumentOutOfRangeException();
				}

			}

			return output;
		}


		public static bool ContainsStateElementsForPage(this PhoneApplicationPage page)
		{
			var pageNameWithPrefix = GetPageName(page) + "_";
			var hasAny = SavedObjects.Any(value => value.Key.StartsWith(pageNameWithPrefix));
			var hasAnyState = CurrentStateDictionary.Any(kvp => kvp.Key.StartsWith(pageNameWithPrefix));

			return hasAny || hasAnyState;
		}

		/// <summary>
		/// Generate a page key for a user state key.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		private static string GenerateKeyFromPageAndKey(PhoneApplicationPage page, string key)
		{
			var name = GetPageName(page);
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
			else if (CurrentStateDictionary.ContainsKey(key))
			{
				result = true;
			}
			else
				result = false;

			return result;
		}

		/// <summary>
		/// Retrieve a page name from page. If PageCommon, uses the PageName property, otherwise uses GetType().Name property.
		/// </summary>
		/// <param name="page">Reference to a PhoneApplicationPage</param>
		/// <returns></returns>
		private static string GetPageName(PhoneApplicationPage page)
		{
			string name;

			if (page is PageCommon)
			{
				var getValue = page.State.GetValueFromDictionary(PageNameTombstoneKey) as string;
				name = getValue ?? ((PageCommon)page).PageName;
			}
			else
			{
				name = page.GetType().Name;
			}

			return name;
		}

		private static StateRestoreData<T> RestoreFromStateDirectly<T>(string key)
		{
			var dict = CurrentStateDictionary;
			StateRestoreData<T> result;

			if (dict.ContainsKey(key))
			{
				var getObject = dict[key];

				if (!(getObject is string))
				{
					result = new StateRestoreData<T>(StateRestoreData<T>.ErrorMessage.NotFound, default(T));
				}
				else
					try
					{

						var jsonObject = JsonConvert.DeserializeObject<T>(getObject as string);
						result = new StateRestoreData<T>(StateRestoreData<T>.ErrorMessage.NoError, jsonObject);
					}
					catch (Exception e)
					{
						result = new StateRestoreData<T>(StateRestoreData<T>.ErrorMessage.WrongType, default(T));
					}
			}
			else
			{
				result = new StateRestoreData<T>(StateRestoreData<T>.ErrorMessage.NotFound, default(T));
			}


			return result;
		}

		private sealed class StateRestoreData<T>
		{
			public enum ErrorMessage
			{
				NoError = 0,
				NotFound,
				WrongType,
			}


			private readonly T _currentValue;
			private readonly ErrorMessage _statusMessage;


			public StateRestoreData(ErrorMessage statusMessage, T value)
			{
				_currentValue = value;
				_statusMessage = statusMessage;
			}


			public ErrorMessage StatusMessage
			{
				get { return _statusMessage; }
			}

			public T CurrentValue
			{
				get { return _currentValue; }
			}
		}

	}
}
;