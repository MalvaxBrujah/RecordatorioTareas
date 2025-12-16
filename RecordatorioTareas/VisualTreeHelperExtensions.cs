using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RecordatorioTareas
{
    public static class VisualTreeHelperExtensions
    {
        public static IEnumerable<T> FindChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
