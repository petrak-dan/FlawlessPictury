using System;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Schedules actions onto the UI thread (or equivalent UI synchronization context).
    /// </summary>
    public interface IUiDispatcher
    {
        /// <summary>
        /// Runs an action on the UI thread.
        /// </summary>
        /// <param name="action">Action to run.</param>
        void Invoke(Action action);

        /// <summary>
        /// Runs an action asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">Action to run.</param>
        void BeginInvoke(Action action);
    }
}
