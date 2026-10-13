namespace KawaSys.Desktop.Forms;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "KawaSys";
        Size = new Size(420, 340);
        StartPosition = FormStartPosition.CenterScreen;

        // Dock.Top apila al revés: se agregan de abajo hacia arriba.
        Agregar("Listado mensual de facturas", () => new ReporteMensualForm());
        Agregar("Cobros y pagos pendientes", () => new CobrosForm());
        Agregar("Facturas", () => new FacturasForm());
        Agregar("Registrar venta", () => new VentaForm());
        Agregar("Motos (ABMC)", () => new MotosForm());
    }

    private void Agregar(string texto, Func<Form> crear)
    {
        var btn = new Button { Text = texto, Dock = DockStyle.Top, Height = 54 };
        btn.Click += (_, _) => { using var f = crear(); f.ShowDialog(this); };
        Controls.Add(btn);
    }
}
