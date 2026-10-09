using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Operacional;

internal static class GridManualRefresh
{
    public static bool PodeAtualizar(RadGridView grid)
    {
        if (!grid.IsEnabled || !grid.IsHitTestVisible || EmEdicao(grid))
        {
            MessageBox.Show("Conclua ou cancele a edição da linha antes de atualizar.", "Atualizar",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
        return true;
    }

    private static bool EmEdicao(DependencyObject element)
    {
        if (element is GridViewRow row && row.IsInEditMode) return true;
        if (element is RadGridView grid && grid.Items is IEditableCollectionView view &&
            (view.IsEditingItem || view.IsAddingNew)) return true;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            if (EmEdicao(VisualTreeHelper.GetChild(element, i))) return true;
        return false;
    }

    public static async Task AtualizarAsync(RadGridView grid, Func<Task> carregar)
    {
        var filtros = grid.FilterDescriptors.ToArray();
        grid.IsEnabled = false;
        grid.IsBusy = true;
        try { await carregar(); }
        finally
        {
            // Keep the original column descriptors, including distinct-value selections.
            foreach (var filtro in filtros)
                if (!grid.FilterDescriptors.Contains(filtro)) grid.FilterDescriptors.Add(filtro);
            grid.IsBusy = false;
            grid.IsEnabled = true;
        }
    }
}
