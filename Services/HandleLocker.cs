using System;
using System.Security;
using System.Threading;

namespace Where.Common.Services
{
	/// <summary>
	/// An encapsulation class containing a reference on a object for accessing with thread safety. Resource reference cannot be changed
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class HandleLocker<T>
	{
		private readonly object _lockObject;
		private T _lockedResource;

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
		protected void Lock()
		{
			Monitor.Enter(_lockObject);
			_locked = true;

		}

		public bool GetLockState
		{
			get { return _locked; }
		}

		protected T AquireResource
		{
			get
			{
				if (!_locked)
					throw new InvalidOperationException("Cannot aquire an resource if not locked");

				return _lockedResource;
			}
		}

		[SecurityCritical]
		private void SetNewObjectValue(T newResource)
		{
			Lock();

			_lockedResource = newResource;

			Release();
		}

		[SecurityCritical]
		protected void Release()
		{
			if (!_locked)
				return;

			Monitor.Pulse(_lockObject);
			Monitor.Exit(_lockObject);
			_locked = false;

		}


		/// <summary>
		/// Execute an operation on a locked resource with internal aquire-release and return a result of type <b>TResult</b>. Operation is NOT reentrable
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="executableAction"></param>
		/// <returns></returns>
		public TResult ExecuteSafeOperationOnObject<TResult>(Func<T, TResult> executableAction)
		{
			Lock();

			var result = executableAction(AquireResource);

			Release();

			return result;

		}

	}
}
