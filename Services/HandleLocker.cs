using System;
using System.Security;
using System.Threading;

namespace Where.Common.Services
{
	public class HandleLocker<T>
	{
		private readonly object _lockObject;
		private readonly T _lockedResource;

		/// <summary>
		/// Create a new resource handler that can only be accessed if you lock and aquire a handle
		/// </summary>
		/// <param name="lockedResource"></param>
		/// <param name="lockObject"></param>
		public HandleLocker(T lockedResource, object lockObject)
		{
			Utils.NotNullArgument(lockObject, "lockObject", "cannot lock on to null");
			_lockObject = lockObject;
			_lockedResource = lockedResource;
		}

		private volatile bool _locked;

		[SecurityCritical]
		public void Lock()
		{
			Monitor.Enter(_lockObject);
			_locked = true;

		}

		public bool GetLockState
		{
			get { return _locked; }
		}

		public T AquireResource
		{
			get
			{
				if (!_locked)
					throw new InvalidOperationException("Cannot aquire an resource if not locked");

				return _lockedResource;
			}
		}

		[SecurityCritical]
		public void Release()
		{
			if (!_locked)
				return;

			Monitor.Pulse(_lockObject);
			Monitor.Exit(_lockObject);
			_locked = false;

		}

	}
}
