using System;
using System.Windows;
using DesktopMemo.Core.Contracts;

namespace DesktopMemo.App.Views
{
    /// <summary>
    /// 未保存修改的用户操作选择
    /// </summary>
    public enum UnsavedChangesAction
    {
        /// <summary>保存修改</summary>
        Save,
        /// <summary>丢弃修改</summary>
        Discard,
        /// <summary>取消操作</summary>
        Cancel
    }

    /// <summary>
    /// 未保存修改确认对话框
    /// </summary>
    public partial class UnsavedChangesDialog : Window
    {
        /// <summary>
        /// 用户选择的操作
        /// </summary>
        public UnsavedChangesAction Action { get; private set; } = UnsavedChangesAction.Cancel;

        /// <summary>
        /// 本地化服务
        /// </summary>
        public ILocalizationService LocalizationService { get; }

        public UnsavedChangesDialog(ILocalizationService localizationService)
        {
            LocalizationService = localizationService;
            InitializeComponent();
            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Action = UnsavedChangesAction.Save;
            SafeCloseDialog(true);
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            Action = UnsavedChangesAction.Discard;
            SafeCloseDialog(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Action = UnsavedChangesAction.Cancel;
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
                try
                {
                    Hide();
                }
                catch
                {
                    // 最后的安全措施
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnsavedChangesDialog SafeCloseDialog error: {ex.Message}");
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
