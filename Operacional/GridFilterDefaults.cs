using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;

namespace Operacional;

internal static class GridFilterDefaults
{
    private static bool registered;

    public static void Register()
    {
        if (registered) return;
        registered = true;
        EventManager.RegisterClassHandler(typeof(RadGridView), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnGridLoaded));
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not RadGridView grid || !ReferenceEquals(e.OriginalSource, grid)) return;
        // Loaded can run again when the user returns to a tab.
        grid.FilterOperatorsLoading -= OnFilterOperatorsLoading;
        grid.FilterOperatorsLoading += OnFilterOperatorsLoading;
    }

    private static void OnFilterOperatorsLoading(object? sender, FilterOperatorsLoadingEventArgs e)
    {
        if (!e.AvailableOperators.Contains(FilterOperator.Contains)) return;
        e.DefaultOperator1 = FilterOperator.Contains;
        e.DefaultOperator2 = FilterOperator.Contains;
    }
}
