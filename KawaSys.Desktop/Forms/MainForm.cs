namespace KawaSys.Desktop.Forms;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "KawaSys";
        Size = new Size(420, 240);
        StartPosition = FormStartPosition.CenterScreen;

        var btnMotos = new Button { Text = "Motos (ABMC)", Dock = DockStyle.Top, Height = 60 };
        var btnVenta = new Button { Text = "Registrar venta", Dock = DockStyle.Top, Height = 60 };
        btnMotos.Click += (_, _) => new MotosForm().ShowDialog(this);
        btnVenta.Click += (_, _) => new VentaForm().ShowDialog(this);

        Controls.Add(btnVenta);
        Controls.Add(btnMotos);
    }
}
