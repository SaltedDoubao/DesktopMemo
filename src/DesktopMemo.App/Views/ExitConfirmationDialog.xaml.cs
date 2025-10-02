using System;
using System.Windows;
using DesktopMemo.Core.Contracts;

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
        public ILocalizationService LocalizationService { get; }

        public ExitConfirmationDialog(ILocalizationService localizationService)
        {
            LocalizationService = localizationService;
            InitializeComponent();
            DataContext = this;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DontShowAgain = DontShowAgainCheckBox?.IsChecked ?? false;
                Action = ExitAction.MinimizeToTray;
                
                // 安全地设置DialogResult和关闭对话框
                SafeCloseDialog(true);
            }
            catch (Exception ex)
            {
                // 记录异常但确保对话框能关闭
                System.Diagnostics.Debug.WriteLine($"ExitConfirmationDialog MinimizeButton error: {ex.Message}");
                SafeCloseDialog(true);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DontShowAgain = DontShowAgainCheckBox?.IsChecked ?? false;
                Action = ExitAction.Exit;
                
                // 安全地设置DialogResult和关闭对话框
                SafeCloseDialog(true);
            }
            catch (Exception ex)
            {
                // 记录异常但确保对话框能关闭
                System.Diagnostics.Debug.WriteLine($"ExitConfirmationDialog ExitButton error: {ex.Message}");
                SafeCloseDialog(true);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DontShowAgain = false;
            Action = ExitAction.None;
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
                System.Diagnostics.Debug.WriteLine($"ExitConfirmationDialog SafeCloseDialog error: {ex.Message}");
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
