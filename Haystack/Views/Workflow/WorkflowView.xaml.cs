using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;
using Haystack.Views.Companies;
using Haystack.Views.Contacts;

namespace Haystack.Views.Workflow;

public partial class WorkflowView : UserControl
{
    private readonly WorkflowViewModel _vm = new();

    public WorkflowView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }

    private void AddTask_Click(object sender, RoutedEventArgs e) => _vm.AddTask();

    // Enter adds without leaving the box, so queueing several tasks is rapid-fire.
    private void NewTask_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) _vm.AddTask();
    }

    private void TaskDone_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: WorkflowTask task }) return;
        _vm.Complete(task);
    }

    private void TaskLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: WorkflowTask { HasTarget: true } task }) return;

        MainWindow.ShowModal(task.TargetKind switch
        {
            RecordKind.Company => new CompanyDetailView(task.TargetId!.Value),
            RecordKind.Contact => new ContactDetailView(task.TargetId!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(task)),
        });

        // The monitors ran when that modal closed (activity may have resolved
        // this or other tasks), so reload the queue to match.
        _vm.LoadCommand.Execute(null);
    }
}
