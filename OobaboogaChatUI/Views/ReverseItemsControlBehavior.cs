using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OobaboogaChatUI.Views;

public class ReverseItemsControlBehavior
{
    public static DependencyProperty ReverseItemsControlProperty =
        DependencyProperty.RegisterAttached("ReverseItemsControl",
            typeof(bool),
            typeof(ReverseItemsControlBehavior),
            new FrameworkPropertyMetadata(false, OnReverseItemsControlChanged));

    public static bool GetReverseItemsControl(DependencyObject obj)
    {
        return (bool)obj.GetValue(ReverseItemsControlProperty);
    }

    public static void SetReverseItemsControl(DependencyObject obj, object value)
    {
        obj.SetValue(ReverseItemsControlProperty, value);
    }

    private static void OnReverseItemsControlChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            var itemsControl = sender as ItemsControl;
            if (itemsControl.IsLoaded)
            {
                DoReverseItemsControl(itemsControl);
            }
            else
            {
                RoutedEventHandler loadedEventHandler = null;
                loadedEventHandler = (sender2, e2) =>
                {
                    itemsControl.Loaded -= loadedEventHandler;
                    DoReverseItemsControl(itemsControl);
                };
                itemsControl.Loaded += loadedEventHandler;
            }
        }
    }

    private static void DoReverseItemsControl(ItemsControl itemsControl)
    {
        var itemPanel = GetItemsPanel(itemsControl);
        itemPanel.LayoutTransform = new ScaleTransform(1, -1);
        Style itemContainerStyle;
        if (itemsControl.ItemContainerStyle == null)
            itemContainerStyle = new Style();
        else
            itemContainerStyle = CopyStyle(itemsControl.ItemContainerStyle);
        var setter = new Setter();
        setter.Property = FrameworkElement.LayoutTransformProperty;
        setter.Value = new ScaleTransform(1, -1);
        itemContainerStyle.Setters.Add(setter);
        itemsControl.ItemContainerStyle = itemContainerStyle;
    }

    private static Panel GetItemsPanel(ItemsControl itemsControl)
    {
        var itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
        if (itemsPresenter == null)
            return null;
        return GetVisualChild<Panel>(itemsControl);
    }

    private static Style CopyStyle(Style style)
    {
        var styleCopy = new Style();
        foreach (var currentSetter in style.Setters) styleCopy.Setters.Add(currentSetter);
        foreach (var currentTrigger in style.Triggers) styleCopy.Triggers.Add(currentTrigger);
        return styleCopy;
    }

    private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
    {
        var child = default(T);

        var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < numVisuals; i++)
        {
            var v = (Visual)VisualTreeHelper.GetChild(parent, i);
            child = v as T;
            if (child == null) child = GetVisualChild<T>(v);
            if (child != null) break;
        }

        return child;
    }
}