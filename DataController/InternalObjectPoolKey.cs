using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Where.Common.DataController.Interfaces;

namespace Where.Common.DataController
{
	internal class InternalObjectPool<T> where T : class , IPrivatePoolMember
	{
		private Dictionary<string, T> _poolMembers = new Dictionary<string, T>();

		private readonly int _maximumPoolMembers;

		public InternalObjectPool(int maxPool)
		{
			_maximumPoolMembers = maxPool;
		}

	}
}
