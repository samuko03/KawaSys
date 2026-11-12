using System;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.Forms;

namespace KawaSysWF
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Red de seguridad: un error inesperado se informa con un mensaje en lugar de cerrar el sistema.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => MostrarError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { if (e.ExceptionObject is Exception ex) MostrarError(ex); };

            // Inicializar base de datos
            DBHelper.Initialize();

            // Login
            var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.LoggedUser == null)
                return;

            Application.Run(new MainForm(login.LoggedUser));
        }

        private static void MostrarError(Exception ex)
        {
            // Las reglas de negocio llegan como InvalidOperationException: se muestran como aviso, no como falla.
            bool regla = ex is InvalidOperationException;
            MessageBox.Show(regla ? ex.Message : "Ocurrió un error inesperado y la operación no se completó:\n\n" + ex.Message,
                regla ? "Validación" : "KawaSys — Error",
                MessageBoxButtons.OK, regla ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }
    }
}
