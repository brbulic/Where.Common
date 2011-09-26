using System;
using Where.Common.Messaging;
using Where.Common.DataController.Interfaces;

namespace Where.Common.DataController
{
	internal static class DataControllerConfig
	{
		/// <summary>
		/// Directory to save shared data.
		/// </summary>
		public const string SharedDataDirectory = "/Configuration/Data/";

		/// <summary>
		/// Creates a subdir to store public properties of a certain data type.
		/// </summary>
		/// <param name="type">Data type containing properties</param>
		/// <returns>Directory for json files of each property in the controlled <b>type</b>.</returns>
		public static string CreateDataDirectoryFromControlledData(Type type)
		{
			var dir = string.Format("{0}{1}", SharedDataDirectory, type.Name);
			IsolatedStorageBase.CreateDirectory(dir);
			return dir;
		}
	}

	public interface IDataObject<T>
	{
		string Key { get; }

		T GetValue();

		void SetValue(T newValue);

		ISuperintendent Controller { get; }
	}


	/// <summary>
	/// Object of this kind is returned from serialization
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class SuperindententDataObject<T> : IPrivatePoolMember
	{
		private readonly string _internalObjectPoolString;

		public SuperintendentStatus CurrentObjectStatus { get; internal set; }

		public T Value { get; internal set; }

		internal SuperindententDataObject(SuperintendentStatus status, T value, string internalObjectPoolString)
		{
			CurrentObjectStatus = status;
			Value = value;
			_internalObjectPoolString = internalObjectPoolString;
		}

		#region Implementation of IPrivatePoolMember

		public string InternalPoolKey
		{
			get { return _internalObjectPoolString; }
		}

		public bool IsUsed
		{
			get;
			set;
		}

		#endregion


		private static SuperindententDataObject<T> _default;

		internal static SuperindententDataObject<T> Default()
		{
			if (_default == null)
				_default = new SuperindententDataObject<T>(SuperintendentStatus.Unknown, default(T), String.Empty);

			return _default;
		}
	}

	/// <summary>
	/// This is a DataObject forwarded with additional serialization
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class DataObject<T> : IDataObject<T>
	{
		private readonly string _key;
		private readonly T _value;
		private readonly ISuperintendent _privateController;


		public DataObject(string key, ISuperintendent controllerInjection, T defaultValue = default(T))
		{
			_key = key;
			_value = defaultValue;
			_privateController = controllerInjection;
		}

		#region Implementation of IDataObject<T>

		public string Key
		{
			get { return _key; }
		}

		public T GetValue()
		{
			var value = _privateController.GetValue(_key, _value);

			return value;
		}

		public void SetValue(T newValue)
		{
			_privateController.SetValue(_key, newValue);
		}

		public ISuperintendent Controller
		{
			get { return _privateController; }
		}


		#endregion
	}

	internal enum PersistenceTarget
	{
		Unknown = 0,
		Default,
		JsonIsolatedStorage,
		XmlIsolatedStorage,
		JsonApplicationState,
		XmlApplicationState,
	}


	public class SuperintendentMessage<T> : MessageBase
	{
		private readonly SuperintendentStatus _status;
		private readonly T _value;
		private readonly string _propertyName;

		public string PropertyName
		{
			get { return _propertyName; }
		}

		public T Value
		{
			get { return _value; }
		}

		public SuperintendentStatus Status
		{
			get { return _status; }
		}

		public static SuperintendentMessage<T> CreateMessage(object sender, string propertyName, SuperintendentStatus status, T value)
		{
			return new SuperintendentMessage<T>(sender, propertyName, status, value);
		}

		private SuperintendentMessage(object sender, string propertyName, SuperintendentStatus status, T value)
			: base(sender, null)
		{
			_status = status;
			_propertyName = propertyName;
			_value = value;
		}
	}

	public delegate void SuperintendentMessageDelegate<T>(SuperintendentMessage<T> messageArgs);
}
