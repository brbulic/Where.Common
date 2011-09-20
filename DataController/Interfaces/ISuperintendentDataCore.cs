using System;

namespace Where.Common.DataController.Interfaces
{
	/// <summary>
	/// Makes sure everything is in sync.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ISuperintendentDataCore<T> where T : class
	{
		T ControlledInstance { get; }

		void SaveValue<TE>(string propertyName, TE value);
		
		SuperindententDataObject<TE> RetrieveValue<TE>(string propertyName, TE defaultValue = default(TE));

		void RetrieveValueAsync<TE>(string propertyName, TE defaultValue, Action<SuperindententDataObject<TE>, object> returns, object state);
	}
}