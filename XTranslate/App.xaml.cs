using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace XTranslate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()

        {

            DispatcherUnhandledException += App_DispatcherUnhandledException;

        }


        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)

        {

            // Handle the exception here

            // You can log the error, display an error message to the user, etc.

            Console.WriteLine("An unhandled exception occurred: " + e.Exception.Message);

            e.Handled = true;

        }
    }    
}
