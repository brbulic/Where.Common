using System.ComponentModel;

namespace Where.Common.DataController.Interfaces
{
	public interface ISuperintendentDataContainer : INotifyPropertyChanged
	{
		bool Initialized { get; }
	}
}
