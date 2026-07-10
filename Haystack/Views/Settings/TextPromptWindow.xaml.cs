using System.Windows;
using System.Windows.Input;

namespace Haystack.Views.Settings;

/// <summary>Small reusable "type a value" prompt (WPF has no built-in InputBox).</summary>
public partial class TextPromptWindow : Window
{
    public string Value => InputField.Text;

    public TextPromptWindow(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();

        TitleText.Text = title;
        PromptText.Text = prompt;
        InputField.Text = initialValue;

        Loaded += (_, _) =>
        {
            InputField.Focus();
            InputField.SelectAll();
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
