using System;
using System.Windows.Controls;

namespace Little_System_Cleaner.Misc
{
    class DynamicTabControl : TabControl
    {
        /// <summary>
        /// A force exit is being done if set to true
        /// </summary>
        public bool ForceExit { get; set; }

        /// <summary>
        /// Changes the selected index by checking if it can be changed first
        /// </summary>
        /// <exception cref="UnloadBlockedException">Thrown if tab cannot be changed</exception>
        public new int SelectedIndex
        {
            get { return base.SelectedIndex; }
            set
            {
                var oldItem = Items[base.SelectedIndex] as TabItem;
                var newItem = Items[value] as TabItem;

                CheckTabChange(oldItem, newItem);

                base.SelectedIndex = value;
            }
        }

        /// <summary>
        /// Changes the selected item by checking if it can be changed first
        /// </summary>
        /// <exception cref="UnloadBlockedException">Thrown if tab cannot be changed</exception>
        public new object SelectedItem
        {
            get { return base.SelectedItem; }
            set
            {
                var oldItem = base.SelectedItem as TabItem;
                var newItem = value as TabItem;

                CheckTabChange(oldItem, newItem);

                base.SelectedItem = value;
            }
        }

        /// <summary>
        /// Checks if TabItem can be unloaded
        /// </summary>
        /// <param name="item">TabItem</param>
        /// <returns>True if it can be unloaded</returns>
        public bool CanUnload(ContentControl item)
        {
            bool? canUnload = null;
            var oldUserControl = ContentToUserControl(item?.Content, true);

            var methodUnload = oldUserControl?.GetType().GetMethod("OnUnloaded");
            if (methodUnload != null)
                canUnload = (bool?)methodUnload.Invoke(oldUserControl, new object[] { ForceExit });

            return !canUnload.HasValue || canUnload == true;
        }
        
        /// <summary>
        /// Checks if tab can be changed to new item
        /// </summary>
        /// <param name="oldItem">Old TabItem (can be null)</param>
        /// <param name="newItem">New TabItem (cannot be null)</param>
        /// <exception cref="UnloadBlockedException">Thrown if old TabItem cannot be unloaded</exception>
        private void CheckTabChange(ContentControl oldItem, ContentControl newItem)
        {
            // Call OnUnloaded method and clear
            if (!CanUnload(oldItem))
                throw new UnloadBlockedException();

            (oldItem?.Content as DynamicUserControl)?.ClearUserControl();

            // Initialize and call OnLoaded method
            var newCtrl = ContentToUserControl(newItem?.Content, false);

            var methodLoad = newCtrl?.GetType().GetMethod("OnLoaded");
            methodLoad?.Invoke(newCtrl, new object[] { });
        }
        
        /// <summary>
        /// Gets UserControl from DynamicUserControl or UserControl
        /// </summary>
        /// <param name="content">Object to convert</param>
        /// <param name="hasContent">Does DynamicUserControl have content?</param>
        /// <returns>UserControl instance or null if it couldnt be converted</returns>
        private static UserControl ContentToUserControl(object content, bool hasContent)
        {
            var userCtrl = content as UserControl;

            if (!(userCtrl is DynamicUserControl))
                return userCtrl;

            if (hasContent)
                // Already has content so use that
                userCtrl = (UserControl) (userCtrl as DynamicUserControl).Content;
            else
                // No content so initialize it
                userCtrl = (userCtrl as DynamicUserControl).InitUserControl();
            
            return userCtrl;
        }
        

        public class UnloadBlockedException : Exception
        {
            public UnloadBlockedException() : base("The current control is blocked from unloading")
            {
            }
        }
    }
}
