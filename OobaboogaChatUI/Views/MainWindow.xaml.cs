using System;
using System.Windows;

namespace OobaboogaChatUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                MessageBox.Show(((Exception)args.ExceptionObject).Message, "Unhandled Exception", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };
        }
    }
}
