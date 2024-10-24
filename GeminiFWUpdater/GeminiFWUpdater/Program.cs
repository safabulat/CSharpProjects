using GeminiReaderUpdaterGUI;
using System;
using System.Windows.Forms;

namespace GeminiFWUpdater
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            MainUpdaterForm mainForm = new MainUpdaterForm();
            Application.Run(mainForm);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {((Exception)e.ExceptionObject).Message}\n\n{((Exception)e.ExceptionObject).StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }
}
