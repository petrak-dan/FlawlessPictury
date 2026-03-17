using System;
using System.Windows.Forms;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Presentation.WinForms.CrossCutting
{
    /// <summary>
    /// Dispatches actions onto the WinForms UI thread using a <see cref="Control"/>.
    /// </summary>
    public sealed class WinFormsUiDispatcher : IUiDispatcher
    {
        private readonly Control _control;

        /// <summary>
        /// Initializes the dispatcher.
        /// </summary>
        public WinFormsUiDispatcher(Control control)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
        }

        /// <inheritdoc />
        public void Invoke(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (_control.InvokeRequired)
            {
                _control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <inheritdoc />
        public void BeginInvoke(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (_control.IsDisposed)
            {
                return;
            }

            if (_control.InvokeRequired)
            {
                _control.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
