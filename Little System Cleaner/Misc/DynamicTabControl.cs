using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;

namespace Little_System_Cleaner.Misc
{
    internal class DynamicTabControl : TabControl
    {
        /// <summary>
        /// A force exit is being done if set to true
        /// </summary>
        public bool ForceExit { get; set; }

        /// <summary>
        /// Stores the previous index to go back to
        /// </summary>
        private int PrevIndex { get; set; }

        private bool IsFirstChange { get; set; } = true;

        /// <summary>
        /// Action to perform when blocked
        /// </summary>
        public Action<DynamicTabControl> DoOnBlocked { get; set; }

        /// <summary>
        /// Goes to previous tab when blocked if true (default is true)
        /// </summary>
        public bool GoBackOnBlocked { get; set; } = true;

        public DynamicTabControl()
        {
            IsSynchronizedWithCurrentItem = true;

            PrevIndex = SelectedIndex >= 0 ? SelectedIndex : 0;
            
            SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (!IsFirstChange && PrevIndex == SelectedIndex)
                return;

            var oldItem = Items[PrevIndex] as ContentControl;
            var newItem = Items[SelectedIndex] as ContentControl;

            try
            {
                CheckTabChange(oldItem, newItem);

                PrevIndex = SelectedIndex;
            }
            catch (UnloadBlockedException)
            {
                if (IsFirstChange)
                    // Shouldn't reach here but just in case
                    throw;

                DoOnBlocked?.Invoke(this);

                if (GoBackOnBlocked)
                    SelectedIndex = PrevIndex;
            }

            IsFirstChange = false;
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
        
        public TabItem GetTabItem(string name)
        {
            try
            {
                return Items.Cast<TabItem>().First(ti => string.CompareOrdinal(ti.Name, name) == 0);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            
        }

        public class UnloadBlockedException : Exception
        {
            public UnloadBlockedException() : base("The current control is blocked from unloading")
            {
            }
        }
    }
}
