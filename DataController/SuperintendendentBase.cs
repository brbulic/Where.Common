using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using Where.Common.Messaging;
using Where.Common.DataController.Interfaces;

namespace Where.Common.DataController
{
	public class SuperintendendentBase<TData> : ISuperintendent where TData : class, ISuperintendentDataContainer
	{
		private static readonly PropertyInfo[] TypeProperties = typeof(TData).GetProperties(BindingFlags.Instance | BindingFlags.Public);

		private readonly ISuperintendentDataCore<TData> _privateDataCore;

		private readonly IMessenger _messengerInstance;

		private readonly IDictionary<string, object> _messengerTokens = new Dictionary<string, object>();

		protected SuperintendendentBase(ISuperintendentDataCore<TData> coreInjector, IMessenger messengerInjector)
		{
			_privateDataCore = coreInjector;
			_messengerInstance = messengerInjector;
			CreateTokens();
		}

		private void CreateTokens()
		{
			foreach (var typeProperty in TypeProperties)
			{
				_messengerTokens.Add(typeProperty.Name, new object());
			}

			Preinit();
		}

		public virtual void Preinit()
		{

		}

		#region Implementation of ISuperintendent

		public T GetValue<T>(string key, T defaultValue = default(T))
		{
			var value = _privateDataCore.RetrieveValue(key, defaultValue);

			if (value.CurrentObjectStatus == SuperintendentStatus.StatusOk)
				return value.Value;

			return defaultValue;
		}

		private struct SmallCompositor<T>
		{
			private readonly string _propertyName;
			private readonly SuperintendentMessageDelegate<T> _delegate;

			public SmallCompositor(string propertyName, SuperintendentMessageDelegate<T> @delegate)
				: this()
			{
				_propertyName = propertyName;
				_delegate = @delegate;
			}

			public SuperintendentMessageDelegate<T> Delegate
			{
				get { return _delegate; }
			}

			public string PropertyName
			{
				get { return _propertyName; }
			}
		}

		public void BeginGetValue<T>(string key, SuperintendentMessageDelegate<T> onMessage, T defaultValue)
		{
			_privateDataCore.RetrieveValueAsync(key, defaultValue, OnDataRecieved, new SmallCompositor<T>(key, onMessage));
		}

		private void OnDataRecieved<T>(SuperindententDataObject<T> arg1, object arg2)
		{
			var transferedData = (SmallCompositor<T>)arg2;

			transferedData.Delegate(arg1.CurrentObjectStatus == SuperintendentStatus.StatusOk
						? SuperintendentMessage<T>.CreateMessage(this, transferedData.PropertyName, SuperintendentStatus.StatusOk, arg1.Value)
						: SuperintendentMessage<T>.CreateMessage(this, transferedData.PropertyName, arg1.CurrentObjectStatus, arg1.Value));
		}


		public void SetValue<T>(string key, T value)
		{
			var oldValue = _privateDataCore.RetrieveValue(key, value);

			if (!EqualityComparer<T>.Default.Equals(oldValue.Value, value))
			{
				_privateDataCore.SaveValue(key, value);
				_messengerInstance.Send(SuperintendentMessage<T>.CreateMessage(this, key, SuperintendentStatus.Changed, value), _messengerTokens[key]);
			}
			else
			{
				_messengerInstance.Send(SuperintendentMessage<T>.CreateMessage(this, key, SuperintendentStatus.StatusOk, value), _messengerTokens[key]);
			}

		}

		public void RegisterForResource<T>(object reciever, string key, SuperintendentMessageDelegate<T> onMessage)
		{
			ContainsPropertyOfType(typeof(T), key);

			Action<SuperintendentMessage<T>> action = myValue => onMessage(myValue); // MONAD BITCH!
			_messengerInstance.Register(reciever, _messengerTokens[key], action);
		}

		public void UnregisterFromResource<T>(object reciever, string key)
		{
			ContainsPropertyOfType(typeof(T), key);
			_messengerInstance.Unregister<SuperintendentMessage<T>>(reciever, _messengerTokens[key]);
		}

		protected void SendStatusMessage<T>(string key, SuperintendentStatus status)
		{
			ContainsPropertyOfType(typeof(T), key);
			var value = _privateDataCore.RetrieveValue<T>(key).Value;
			_messengerInstance.Send(SuperintendentMessage<T>.CreateMessage(this, key, status, value), _messengerTokens[key]);
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Throws exception if nonexistant property
		/// </summary>
		/// <param name="type"></param>
		/// <param name="propertyName"></param>
		public static void ContainsPropertyOfType(Type type, string propertyName)
		{
			var foundProp = TypeProperties.Where(property => property.Name.Equals(propertyName)).First();

			if (foundProp == null)
				throw new ArgumentOutOfRangeException("propertyName", String.Format("Nonexistant property \"{0}\".", propertyName));

			if (type != typeof(object))
				if (!foundProp.PropertyType.Equals(type))
					throw new ArgumentOutOfRangeException("type", String.Format("Property \"{0}\" is requested for a wrong type.", propertyName));
		}

		public static PropertyInfo GetPropertyInfoForName(string propertyName)
		{
			ContainsPropertyOfType(GetPropertyType(propertyName), propertyName);
			var info = TypeProperties.First(prop => prop.Name.Equals(propertyName));
			return info;
		}

		public static Type GetPropertyType(string propertyName)
		{
			var foundProp = TypeProperties.Where(property => property.Name.Equals(propertyName)).First();
			return foundProp.PropertyType;
		}

		#endregion
	}
}
