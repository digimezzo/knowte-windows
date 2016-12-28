using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Knowte.Common.Utils
{
    public sealed class VisualTreeUtils
    {
        public static IEnumerable<DependencyObject> GetVisuals(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;

                foreach (var descendants in GetVisuals(child))
                {
                    yield return descendants;
                }
            }
        }

        // Helper to search up the VisualTree
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);

            } while (current != null);

            return null;
        }
    }
}
