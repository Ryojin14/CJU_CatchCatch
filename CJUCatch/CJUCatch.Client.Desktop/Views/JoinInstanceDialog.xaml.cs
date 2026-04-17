using System.Windows;
using CJUCatch.Shared;

namespace CJUCatch.Client.Desktop.Views;

public partial class JoinInstanceDialog : Window
{
    public string InstanceCode => CodeTextBox.Text.Trim().ToUpperInvariant();

    public JoinInstanceDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => CodeTextBox.Focus();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (!InputRules.IsValidInstanceCode(InstanceCode))
        {
            MessageBox.Show(
                this,
                $"인스턴스 코드는 {InputRules.InstanceCodeLength}자리 영문 대문자/숫자여야 합니다.",
                "입력 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
