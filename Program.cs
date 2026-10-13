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

            // Inicializar base de datos
            DBHelper.Initialize();

            // Login
            var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.LoggedUser == null)
                return;

            Application.Run(new MainForm(login.LoggedUser));
        }
    }
}
