using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;
using Operacional;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            GridFilterDefaults.Register();
            Run();
            Console.WriteLine("PASS: deferred commit, duplicate prevention, failure/retry, ESC, cancelled queued edit and new-row insertion.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Run()
    {
        var item = new Item { Name = "Original" };
        var grid = new RadGridView
        {
            ItemsSource = new ObservableCollection<Item> { item },
            AutoGenerateColumns = false, ShowGroupPanel = false, CanUserInsertRows = true,
            NewRowPosition = GridViewNewRowPosition.Bottom,
            Width = 500, Height = 300
        };
        grid.Columns.Add(new GridViewDataColumn { DataMemberBinding = new Binding(nameof(Item.Name)) });
        grid.Columns.Add(new GridViewDataColumn { DataMemberBinding = new Binding(nameof(Item.Quantity)) });
        using var source = new HwndSource(new HwndSourceParameters("Grid persistence regression")
        { Width = 500, Height = 300, PositionX = -32000, PositionY = -32000,
          WindowStyle = unchecked((int)0x90000000), ExtendedWindowStyle = 0x08000000 });
        source.RootVisual = grid;
        Pump();
        grid.Measure(new Size(500, 300));
        grid.Arrange(new Rect(0, 0, 500, 300));
        grid.UpdateLayout();
        Pump();
        CheckFilterDefaults(grid);
        int saves = 0, validated = 0;
        var completion = new TaskCompletionSource();
        grid.RowValidating += (_, e) => ValidatedGridSave.Save(grid, e, () =>
        {
            saves++;
            return completion.Task;
        });
        grid.RowValidated += (_, _) => validated++;
        grid.CurrentCellInfo = new GridViewCellInfo(item, grid.Columns[0]);
        grid.CurrentItem = item;
        grid.Focus();
        grid.BeginEdit();
        SetEditor(grid, "Changed");
        grid.CommitEdit();
        Pump();
        Assert(saves == 1 && validated == 0, $"Premature commit or missing save: saves={saves}, validated={validated}");
        grid.CommitEdit();
        Pump();
        Assert(saves == 1, "Duplicate persistence while the first request is running.");
        completion.SetResult();
        Pump();
        Assert(validated == 1, $"The saved edit did not commit exactly once: {validated}");

        completion = new TaskCompletionSource();
        grid.CurrentCellInfo = new GridViewCellInfo(item, grid.Columns[0]);
        grid.BeginEdit();
        SetEditor(grid, "Failed");
        grid.CommitEdit();
        Pump();
        completion.SetException(new InvalidOperationException("Simulated database failure"));
        Pump();
        Assert(ErrorDialog.Errors == 1 && validated == 1, "A failed save committed or did not report its error.");
        grid.CancelEdit();
        Pump();
        Assert(item.Name == "Changed", "ESC did not restore the edit after a failed save.");
        completion = new TaskCompletionSource();
        grid.CurrentCellInfo = new GridViewCellInfo(item, grid.Columns[0]);
        grid.BeginEdit();
        SetEditor(grid, "Retried");
        grid.CommitEdit();
        Pump();
        completion.SetResult();
        Pump();
        Assert(saves == 3 && validated == 2, $"A failed save prevented a later retry: saves={saves}, validated={validated}, errors={ErrorDialog.Errors}, name={item.Name}");

        grid.CurrentCellInfo = new GridViewCellInfo(item, grid.Columns[0]);
        grid.BeginEdit();
        SetEditor(grid, "Cancel before dispatch");
        grid.CommitEdit();
        grid.CancelEdit();
        Pump();
        Assert(saves == 3 && item.Name == "Retried", $"A cancelled queued edit was persisted: saves={saves}, name={item.Name}");

        completion = new TaskCompletionSource();
        grid.CancelEdit();
        var newItem = grid.Items.AddNew();
        Pump();
        grid.CurrentCellInfo = new GridViewCellInfo(newItem, grid.Columns[0]);
        grid.BeginEdit();
        SetEditor(grid, "New record");
        grid.CommitEdit();
        Pump();
        grid.CommitEdit();
        Pump();
        Assert(saves == 4, "A new row was inserted more than once while pending.");
        completion.SetResult();
        Pump();
        Assert(((ObservableCollection<Item>)grid.ItemsSource).Count == 2 && validated == 3,
            $"The new row was not committed exactly once: count={((ObservableCollection<Item>)grid.ItemsSource).Count}, saved={saves}, validated={validated}, errors={ErrorDialog.Errors}");
    }

    private static void CheckFilterDefaults(RadGridView grid)
    {
        int checkedColumns = 0;
        grid.FilterOperatorsLoading += (_, e) =>
        {
            if (e.Column == grid.Columns[0])
                Assert(e.DefaultOperator1 == Telerik.Windows.Data.FilterOperator.Contains &&
                    e.DefaultOperator2 == Telerik.Windows.Data.FilterOperator.Contains,
                    "Text filters did not default to Contains.");
            else
                Assert(e.DefaultOperator1 != Telerik.Windows.Data.FilterOperator.Contains,
                    "Numeric filters received an incompatible default.");
            checkedColumns++;
        };
        foreach (var column in grid.Columns)
        {
            var filter = new FilteringControl();
            filter.Prepare(column);
        }
        Assert(checkedColumns == 2, $"Expected two filter controls, got {checkedColumns}.");
        Console.WriteLine("PASS: text filters default to Contains; numeric filters retain compatible operators.");
    }

    private static void SetEditor(RadGridView grid, string value)
    {
        Pump();
        var editor = grid.ChildrenOfType<System.Windows.Controls.TextBox>().FirstOrDefault(t => t.IsVisible);
        if (editor == null)
            throw new InvalidOperationException($"No editor. Loaded={grid.IsLoaded}, visible={grid.IsVisible}, rows={grid.ChildrenOfType<GridViewRow>().Count()}");
        editor.Text = value;
        editor.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
    }

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public sealed class Item
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
    }
}

namespace Operacional
{
    internal static class ErrorDialog
    {
        public static int Errors;
        public static void Show(Exception exception, string title) => Errors++;
    }
}
