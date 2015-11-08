using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Misc
{
    public abstract class WizardBase : UserControl
    {
        public List<Type> Controls { get; } = new List<Type>();

        public UserControl CurrentControl => (UserControl) Content;

        /// <summary>
        ///     This value is readonly and can only be manipulated with SetCurrentControl()
        /// </summary>
        public int CurrentControlIndex { get; private set; }

        /// <summary>
        ///     This function is called when the wizard is loaded
        /// </summary>
        public abstract void OnLoaded();

        /// <summary>
        ///     This function is called if the tab is being changed or the program is trying to exit
        /// </summary>
        /// <param name="forceExit">If true, the program is being forced to exit and the return value will have no effect</param>
        /// <returns>If true, the wizard can be unloaded. Otherwise if false, the wizard will stay loaded</returns>
        public abstract bool OnUnloaded(bool forceExit);

        /// <summary>
        ///     Moves the wizard to the first control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveFirst(bool autoMove = true)
        {
            SetCurrentControl(0, autoMove);
        }

        /// <summary>
        ///     Moves the wizard to the previous control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MovePrev(bool autoMove = true)
        {
            var prevControl = CurrentControlIndex - 1;

            SetCurrentControl(prevControl, autoMove);
        }

        /// <summary>
        ///     Moves the wizard to the next control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveNext(bool autoMove = true)
        {
            var nextControl = CurrentControlIndex + 1;

            SetCurrentControl(nextControl, autoMove);
        }

        /// <summary>
        ///     Moves the wizard to the last control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveLast(bool autoMove = true)
        {
            var lastControl = Controls.Count;

            SetCurrentControl(lastControl, autoMove);
        }

        /// <summary>
        ///     Sets the current control index
        /// </summary>
        /// <param name="index">Index of control in list</param>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        /// <remarks>This function can only be called from within the class that inherits this one</remarks>
        protected void SetCurrentControl(int index, bool autoMove = true)
        {
            if (Controls.Count == 0)
                throw new IndexOutOfRangeException(
                    $"There are no controls and therefore #{index} doesn't exist in the controls");

            if (index < 0 || index > Controls.Count)
                throw new IndexOutOfRangeException($"There is no control with #{index}");

            CurrentControlIndex = index;

            if (autoMove)
                ChangeCurrentControl();
        }

        /// <summary>
        ///     Changes the current control
        /// </summary>
        /// <remarks>This function can only be called from within the class that inherits this one</remarks>
        protected void ChangeCurrentControl()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action(ChangeCurrentControl));

                return;
            }

            CurrentControl?.RaiseEvent(new RoutedEventArgs(UnloadedEvent, CurrentControl));

            Content = Activator.CreateInstance(Controls[CurrentControlIndex], this);
        }
    }
}