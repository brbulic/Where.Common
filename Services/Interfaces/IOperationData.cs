namespace Where.Common.Services.Interfaces
{
    /// <summary>
    /// Defines the priority operation interface
    /// </summary>
    public interface IOperationData
    {
        /// <summary>
        /// Priority data
        /// </summary>
        OperationPriority Priority { get; }

        /// <summary>
        /// User state data
        /// </summary>
        object UserState { get; }
    }
}