using System;
using System.IO;
using System.Windows;

namespace xcrosshair
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) => LogFatal(ex.ExceptionObject as Exception);
            DispatcherUnhandledException += (s, ex) => { LogFatal(ex.Exception); ex.Handled = false; };
            base.OnStartup(e);
        }

        private void LogFatal(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fatal.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] FATAL: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }
    }
}
