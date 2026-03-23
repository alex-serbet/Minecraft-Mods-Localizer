using MinecraftLocalizer.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MinecraftLocalizer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDataGridScrollViewer();
            InitializeTextBoxScrollViewer();
            InitializeStreamingScrollViewer();
            InitializeConsoleScrollViewer();
            RefreshAllCustomScrollBars();

            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.StreamingTextScrolled += HandleStreamingTextChanged;
                viewModel.ConsoleOutputScrolled += HandleConsoleTextChanged;
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            StreamingTextBox.Loaded += (_, _) => EnsureStreamingScrollViewer();
            StreamingTextBox.IsVisibleChanged += (_, _) => EnsureStreamingScrollViewer();
            ConsoleTextBox.Loaded += (_, _) => EnsureConsoleScrollViewer();
            ConsoleTextBox.IsVisibleChanged += (_, _) => EnsureConsoleScrollViewer();
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not RichTextBox richTextBox || richTextBox.DataContext is not LocalizationItem item)
                return;

            TextRange range = new(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            item.TranslatedString = range.Text.TrimEnd('\r', '\n');
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.MainViewModel.IsConsoleContentVisible) ||
                e.PropertyName == nameof(ViewModels.MainViewModel.StreamingTextRowHeight) ||
                e.PropertyName == nameof(ViewModels.MainViewModel.IsStreamingButtonCollapsed) ||
                e.PropertyName == nameof(ViewModels.MainViewModel.ShowConsoleOutput))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnsureStreamingScrollViewer();
                    EnsureConsoleScrollViewer();
                    RefreshAllCustomScrollBars();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}
