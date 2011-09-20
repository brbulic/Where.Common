using System.ComponentModel;

namespace Where.Common.DataController.Interfaces
{
	public interface ISuperintendent
	{
		/// <summary>
		/// Get value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		T GetValue<T>(string key, T defaultValue = default(T));

		/// <summary>
		/// Async value get operation
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="onMessage"></param>
		/// <param name="defaultValue"></param>
		void BeginGetValue<T>(string key, SuperintendentMessageDelegate<T> onMessage, T defaultValue = default(T));

		void SetValue<T>(string key, T value);

		void RegisterForResource<T>(object reciever, string key, SuperintendentMessageDelegate<T> onMessage);

		void UnregisterFromResource<T>(object reciever, string key);
	}
}
