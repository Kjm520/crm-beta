using System.Windows;
using System.Windows.Controls;
using Haystack.ViewModels;

namespace Haystack.Views.Companies;

public partial class CompaniesView : UserControl
{
    private readonly CompaniesViewModel _vm = new();

    public CompaniesView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadCommand.Execute(null);
    }

    private void NewCompany_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.ShowModal(new CompanyDetailView(null));
        _vm.LoadCommand.Execute(null);
    }

    private void CompanyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int companyId })
            return;

        MainWindow.ShowModal(new CompanyDetailView(companyId));
        _vm.LoadCommand.Execute(null);
    }
}
