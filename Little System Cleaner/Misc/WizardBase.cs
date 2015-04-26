using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Misc
{
    public abstract class WizardBase : UserControl
    {
        private List<Type> _controls = new List<Type>();
        private int _currentControlIndex = 0;

        public List<Type> Controls
        {
            get { return this._controls; }
        }

        public UserControl CurrentControl
        {
            get { return (UserControl)this.Content; }
        }

        /// <summary>
        /// This value is readonly and can only be manipulated with SetCurrentControl()
        /// </summary>
        public int CurrentControlIndex
        {
            get { return this._currentControlIndex; }
        }

        public WizardBase()
        {
            
        }

        /// <summary>
        /// This function is called when the wizard is loaded
        /// </summary>
        public abstract void OnLoaded();

        /// <summary>
        /// This function is called if the tab is being changed or the program is trying to exit
        /// </summary>
        /// <param name="forceExit">If true, the program is being forced to exit and the return value will have no effect</param>
        /// <returns>If true, the wizard can be unloaded. Otherwise if false, the wizard will stay loaded</returns>
        public abstract bool OnUnloaded(bool forceExit);

        /// <summary>
        /// Moves the wizard to the first control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveFirst(bool autoMove = true)
        {
            this.SetCurrentControl(0, autoMove);
        }

        /// <summary>
        /// Moves the wizard to the previous control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MovePrev(bool autoMove = true)
        {
            int prevControl = this.CurrentControlIndex - 1;

            this.SetCurrentControl(prevControl, autoMove);
        }

        /// <summary>
        /// Moves the wizard to the next control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveNext(bool autoMove = true)
        {
            int nextControl = this.CurrentControlIndex + 1;

            this.SetCurrentControl(nextControl, autoMove);
        }

        /// <summary>
        /// Moves the wizard to the last control
        /// </summary>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        public virtual void MoveLast(bool autoMove = true)
        {
            int lastControl = this.Controls.Count;

            this.SetCurrentControl(lastControl, autoMove);
        }
        
        /// <summary>
        /// Sets the current control index
        /// </summary>
        /// <param name="index">Index of control in list</param>
        /// <param name="autoMove">If true, changes to control without having to call ChangeCurrentControl() after (default: true)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the control list is empty or the index doesn't exist</exception>
        /// <remarks>This function can only be called from within the class that inherits this one</remarks>
        protected void SetCurrentControl(int index, bool autoMove = true)
        {
            if (this.Controls.Count == 0)
                throw new IndexOutOfRangeException(string.Format("There are no controls and therefore #{0} doesn't exist in the controls", index));

            if (index < 0 || index > this.Controls.Count)
                throw new IndexOutOfRangeException(string.Format("There is no control with #{0}", index));

            this._currentControlIndex = index;

            if (autoMove)
                this.ChangeCurrentControl();
        }

        /// <summary>
        /// Changes the current control
        /// </summary>
        /// <remarks>This function can only be called from within the class that inherits this one</remarks>
        protected void ChangeCurrentControl()
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action(() => ChangeCurrentControl()));

                return;
            }

            if (this.CurrentControl != null)
                this.CurrentControl.RaiseEvent(new RoutedEventArgs(UserControl.UnloadedEvent, this.CurrentControl));

            this.Content = Activator.CreateInstance(this.Controls[this.CurrentControlIndex], this);
        }
    }
}
