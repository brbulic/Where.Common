using System.ComponentModel;
using System.Windows.Input;

namespace Where.Common.Controls
{
	public interface IListMenuItem : INotifyPropertyChanged
	{
		ICommand MenuItemClick { get; }
		string CommandTitle { get; }
	}
}
