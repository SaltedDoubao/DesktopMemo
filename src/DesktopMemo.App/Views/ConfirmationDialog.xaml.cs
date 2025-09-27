using System.Windows;

namespace DesktopMemo.App.Views
{
    public partial class ConfirmationDialog : Window
    {
        public bool DontShowAgain { get; private set; }
        public bool? DialogResultValue { get; private set; }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(ConfirmationDialog), new PropertyMetadata(string.Empty));

        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = DontShowAgainCheckBox.IsChecked ?? false;
            DialogResultValue = true;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = false;
            DialogResultValue = false;
            DialogResult = false;
            Close();
        }
    }
}
