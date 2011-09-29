using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Where.Common.DataController.Interfaces
{
	public interface ISuperintendentDataContainer : INotifyPropertyChanged
	{
		bool Initialized { get; }
	}
}
