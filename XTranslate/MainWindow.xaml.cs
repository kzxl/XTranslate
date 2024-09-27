using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using XTranslate.functions;

namespace XTranslate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
        private async void HandleHotkeys(HotkeySettings hotkeySetting)
        {

            await TaskHelpers.ExecuteJob(hotkeySetting.TaskSettings);
        }

    }

}