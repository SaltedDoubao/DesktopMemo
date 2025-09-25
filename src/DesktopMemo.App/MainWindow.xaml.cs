using System.Windows;
using DesktopMemo.App.ViewModels;

namespace DesktopMemo.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}