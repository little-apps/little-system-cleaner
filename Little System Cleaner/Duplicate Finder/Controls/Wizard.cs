using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    public class Wizard : UserControl
    {
        List<Type> arrayControls = new List<Type>();
        int currentControl = 0;

        public UserControl userControl
        {
            get { return (UserControl)this.Content; }
        }

        public Wizard()
        {
            this.arrayControls.Add(typeof(Start));
        }

        public void OnLoaded()
        {
            this.SetCurrentControl(0);
        }

        public bool OnUnloaded()
        {

            return true;
        }

        /// <summary>
        /// Changes the current control
        /// </summary>
        /// <param name="index">Index of control in list</param>
        private void SetCurrentControl(int index)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action(() => SetCurrentControl(index)));
                return;
            }

            if (this.userControl != null)
                this.userControl.RaiseEvent(new RoutedEventArgs(UserControl.UnloadedEvent, this.userControl));

            System.Reflection.ConstructorInfo constructorInfo = this.arrayControls[index].GetConstructor(new Type[] { typeof(Wizard) });

            this.Content = constructorInfo.Invoke(new object[] { this });
        }

        /// <summary>
        /// Moves to the next control
        /// </summary>
        public void MoveNext()
        {
            SetCurrentControl(++currentControl);
        }

        /// <summary>
        /// Moves to the previous control
        /// </summary>
        public void MovePrev()
        {
            SetCurrentControl(--currentControl);
        }

        /// <summary>
        /// Moves to the first control
        /// </summary>
        public void MoveFirst()
        {
            currentControl = 0;

            SetCurrentControl(currentControl);
        }
    }
}
