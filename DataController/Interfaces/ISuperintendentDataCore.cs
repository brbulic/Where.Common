using System;
using System.ComponentModel;

namespace Where.Common.DataController.Interfaces
{
	/// <summary>
	/// Makes sure everything is in sync.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	public interface ISuperintendentDataCore<TData> where TData : class, ISuperintendentDataContainer
	{
		TData ControlledInstance { get; }

		void SaveValue<TElement>(string propertyName, TElement value);

		void StoreCachedValue(string propertyName);

		SuperindententDataObject<TElement> RetrieveValue<TElement>(string propertyName, TElement defaultValue = default(TElement));

		void RetrieveValueAsync<TElement>(string propertyName, TElement defaultValue, Action<SuperindententDataObject<TElement>, object> returns, object state);
	}
}