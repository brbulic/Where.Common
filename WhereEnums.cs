namespace Where.Common.Services
{
	public enum OperationPriority
	{
		Unknown,
		Normal,
		High,
	}

}

namespace Where
{

	/// <summary>
	/// Enumeration for Application state (Launching, Activated, Deactivated, Closing)
	/// </summary>
	public enum CurrentAppState
	{
		/// <summary>
		/// Standard Application State (default)
		/// </summary>
		None,

		/// <summary>
		/// Used when Application is launching normally (started from Home page or Application menu)
		/// </summary>
		Starting,

		/// <summary>
		/// Used when Application is activated from background.
		/// </summary>
		Resuming,

		/// <summary>
		/// Used when the application must use Tombstoned data to restore page state.
		/// </summary>
		ResumingNotPreserved,

		/// <summary>
		/// Used when Application is deactivating (put to background using the Windows key)
		/// </summary>
		Backgrounding,

		/// <summary>
		/// Used when Application is closing (closed using the Back key on MainPage)
		/// </summary>
		Exiting,

		/// <summary>
		/// Used when application is in InvalidState (for errors, unhandled exceptions etc)
		/// </summary>
		InvalidAppState,
	}


}

namespace Where.Common
{
	public enum DataControllerTarget
	{
		Default,
		Memory,
		IsolatedStorageKey,
		IsolatedStorageJson,
	}

	/// <summary>
	/// Signalls the stati of the property
	/// </summary>
	public enum DataState
	{
		/// <summary>
		///  No status. Default value. Signals an framework failure.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Property exists but it's up to date value isn't saved,
		/// </summary>
		NotSaved,

		/// <summary>
		/// Property is empty.
		/// </summary>
		Empty,

		/// <summary>
		/// Property is saved to storage.
		/// </summary>
		Saved,

		/// <summary>
		/// Property is ready to be written.
		/// </summary>
		PendingWrite,

		/// <summary>.
		/// Property is being written.
		/// </summary>
		BeingWritten,
	}

	/// <summary>
	/// Data field status for the Superintendent
	/// </summary>
	public enum SuperintendentStatus
	{
		/// <summary>
		/// No status. Default value. Signals an framework failure.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The stored value is up to date.
		/// </summary>
		StatusOk,

		/// <summary>
		/// Value has been changed. Current value is up to date.
		/// </summary>
		Changed,

		/// <summary>
		/// Signalls that a saved property needs to be reloaded
		/// </summary>
		NeedsReload,

		/// <summary>
		/// Singalls that a saved property is reloading
		/// </summary>
		IsReloading,

		/// <summary>
		/// Signals an framework failure if a property name is not found in the Superintendent
		/// </summary>
		KeyNotFound,

		/// <summary>
		/// If a data isn't found in the storage nor requested from the web.
		/// </summary>
		DataNotFound,
	}

}

namespace Where.Api
{
	/// <summary>
	/// HTTP request type (GET or POST)
	/// </summary>
	public enum RequestType
	{
		Get = 0,
		Post
	};

}

namespace Where.Common.Mvvm
{
	public enum PageStateEnum
	{
		Unknown = 0,
		None,
		Launching,
		Activated,
		Closing,
		Deactivated
	}
}

