using System.Windows;
using System.Windows.Media;

public static class VisualTreeHelperExtensions
{
    public static IEnumerable<T> FindChildren<T>(this DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child != null && child is T t)
                yield return t;

            foreach (var descendant in FindChildren<T>(child))
                yield return descendant;
        }
    }
}
