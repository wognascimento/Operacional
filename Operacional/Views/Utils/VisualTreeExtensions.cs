using System.Windows;
using System.Windows.Media;

namespace Operacional.Views.Utils
{
    public static class VisualTreeExtensions
    {
        public static T? GetParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is T typed)
                    return typed;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        public static T? GetVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is T found)
                    return found;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
