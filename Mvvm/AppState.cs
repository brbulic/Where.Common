using System.Windows;

namespace Where
{
    public static class AppState
    {
        /// <summary>
        /// Returns true if application state is "None" which means that the application start has been handled.
        /// </summary>
        internal static bool IsNormalOperation { get; private set; }
        
        /// <summary>
        /// Sets the current applcation state from the Application's Application Events.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="state"></param>
        public static void SetCurrentAppState(this Application app, CurrentAppState state)
        {
            GetCurrentAppState = state;
            if (state == CurrentAppState.None)
                IsNormalOperation = true;
        }

        /// <summary>
        /// Gets the current application state
        /// </summary>
        public static CurrentAppState GetCurrentAppState { get; private set; }
    }
}
