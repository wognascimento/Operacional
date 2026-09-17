using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Operacional;

// RowValidating is synchronous: hold the edit until persistence has completed.
internal static class ValidatedGridSave
{
    private sealed class State
    {
        public bool Busy;
        public object? CommittingItem;
    }

    private static readonly ConditionalWeakTable<RadGridView, State> States = new();

    public static void Save(object sender, GridViewRowValidatingEventArgs e, Func<Task> persist, Func<Task>? afterCommit = null)
    {
        if (sender is not RadGridView grid || !e.IsValid || e.Row?.IsInEditMode != true)
            return;
        var state = States.GetOrCreateValue(grid);
        var item = e.Row.Item;
        if (ReferenceEquals(state.CommittingItem, item))
            return;
        e.IsValid = false;
        if (state.Busy)
            return;
        state.Busy = true;
        bool cancelled = false;
        EventHandler<GridViewRowEditEndedEventArgs> onEditEnded = (_, args) =>
        {
            if (args.EditAction == GridViewEditAction.Cancel)
                cancelled = true;
        };
        grid.RowEditEnded += onEditEnded;
        grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            if (cancelled || !ReferenceEquals(e.Row.Item, item) || !e.Row.IsInEditMode)
            {
                grid.RowEditEnded -= onEditEnded;
                state.Busy = false;
                return;
            }
            var hitTest = grid.IsHitTestVisible;
            KeyEventHandler blockKeys = (_, args) => args.Handled = true;
            grid.PreviewKeyDown += blockKeys;
            grid.IsHitTestVisible = false;
            try
            {
                await persist();
                state.CommittingItem = item;
                grid.CommitEdit();
                if (afterCommit != null)
                {
                    try { await afterCommit(); }
                    catch (Exception ex) { ErrorDialog.Show(ex, "Dados gravados, mas nao foi possivel atualizar a exibicao."); }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(ex, "Dados nao gravados. Corrija a linha ou pressione ESC.");
            }
            finally
            {
                state.CommittingItem = null;
                state.Busy = false;
                grid.IsHitTestVisible = hitTest;
                grid.PreviewKeyDown -= blockKeys;
                grid.RowEditEnded -= onEditEnded;
            }
        }));
    }
}
