namespace Where.Common.Services.Interfaces
{
	/// <summary>
	/// Defines the priority operation interface
	/// </summary>
	public interface IBackgroundOperationData : IQueueOperationData
	{
		/// <summary>
		/// Priority data
		/// </summary>
		OperationPriority Priority { get; }

	}
}