using System.Windows;

namespace DesktopMemo.App.Views
{
    public enum ExitAction
    {
        None,
        MinimizeToTray,
        Exit
    }

    public partial class ExitConfirmationDialog : Window
    {
        public bool DontShowAgain { get; private set; }
        public ExitAction Action { get; private set; } = ExitAction.None;

        public ExitConfirmationDialog()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = DontShowAgainCheckBox.IsChecked ?? false;
            Action = ExitAction.MinimizeToTray;
            DialogResult = true;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = DontShowAgainCheckBox.IsChecked ?? false;
            Action = ExitAction.Exit;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = false;
            Action = ExitAction.None;
            DialogResult = false;
            Close();
        }
    }
}
