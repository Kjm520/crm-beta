using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haystack.Models;
using Haystack.ViewModels;
using Haystack.Views.Common;

namespace Haystack.Views.Companies;

public partial class CompaniesView : UserControl
{
    private readonly CompaniesViewModel _vm = new();

    public CompaniesView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);

        var spec = (TableColumns)Resources["Spec"];
        ListColumns.Apply(spec, ListColumns.CompaniesKey);
        _ = new TableHeader(HeaderPanel, spec, RowsList, ListColumns.CompaniesKey);
    }

    private void NewCompany_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.ShowModal(new CompanyDetailView(null));
        _vm.LoadCommand.Execute(null);
    }

    private void CompanyRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem { Content: Company company })
            return;

        MainWindow.ShowModal(new CompanyDetailView(company.Id));
        _vm.LoadCommand.Execute(null);
    }
}
