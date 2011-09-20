using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Where.Common.DataController.Interfaces
{
	internal interface IPrivatePoolMember
	{
		string InternalPoolKey { get; }

		bool IsUsed { get; set; }
	}
}
