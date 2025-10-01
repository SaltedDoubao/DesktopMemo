using System;
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
            try
            {
                DontShowAgain = DontShowAgainCheckBox?.IsChecked ?? false;
                DialogResultValue = true;
                
                // 安全地关闭对话框
                SafeCloseDialog(true);
            }
            catch (Exception ex)
            {
                // 记录异常但确保对话框能关闭
                System.Diagnostics.Debug.WriteLine($"ConfirmationDialog YesButton error: {ex.Message}");
                
                // 确保对话框关闭
                SafeCloseDialog(true);
            }
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = false;
            DialogResultValue = false;
            SafeCloseDialog(false);
        }

        private void SafeCloseDialog(bool result)
        {
            try
            {
                DialogResult = result;
                Close();
            }
            catch (InvalidOperationException)
            {
                // DialogResult设置或关闭失败，尝试隐藏
                try
                {
                    Hide();
                }
                catch
                {
                    // 如果隐藏也失败，什么都不做
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfirmationDialog SafeCloseDialog error: {ex.Message}");
                try
                {
                    Hide();
                }
                catch
                {
                    // 最后的安全措施
                }
            }
        }
    }
}
