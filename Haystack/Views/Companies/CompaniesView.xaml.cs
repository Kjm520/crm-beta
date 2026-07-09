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
        var detail = new CompanyDetailView(null);
        detail.ShowDialog();
        _vm.LoadCommand.Execute(null);
    }

    private void CompanyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int companyId })
            return;

        var detail = new CompanyDetailView(companyId);
        detail.ShowDialog();
        _vm.LoadCommand.Execute(null);
    }
}
