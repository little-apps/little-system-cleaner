using System;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Misc
{
    public class DynamicUserControl : UserControl
    {
        public static readonly DependencyProperty TypeProperty = DependencyProperty.RegisterAttached("Type",
            typeof(Type), typeof(DynamicUserControl), new FrameworkPropertyMetadata(null));

        public static Type GetType(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            return (Type)element.GetValue(TypeProperty);
        }

        public static void SetType(UIElement element, Type value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            element.SetValue(TypeProperty, value);
        }

        public UserControl InitUserControl()
        {
            Type controlType = GetType(this);

            if (!controlType.IsClass)
                return null;

            if (!controlType.IsSubclassOf(typeof (UserControl)))
                return null;

            UserControl control = (UserControl)Activator.CreateInstance(controlType);

            Content = control;

            return control;
        }

        public void ClearUserControl()
        {
            Content = null;

            GC.Collect();
        }
    }
}
