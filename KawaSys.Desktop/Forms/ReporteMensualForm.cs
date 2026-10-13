using KawaSys.Desktop.Data;

namespace KawaSys.Desktop.Forms;

/// <summary>Listado mensual de facturas cobradas y pendientes (KAWA-53).</summary>
public class ReporteMensualForm : Form
{
    private readonly FacturaService _facturas = new();
    private readonly NumericUpDown _numAnio = new() { Minimum = 2000, Maximum = 2100, Value = DateTime.Today.Year, Width = 70 };
    private readonly ComboBox _cmbMes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells };
    private readonly Label _lblTotales = new() { AutoSize = true, Margin = new Padding(12, 8, 0, 0) };
    private List<FacturaRow> _filas = [];

    public ReporteMensualForm()
    {
        Text = "Listado mensual de facturas";
        Size = new Size(1000, 560);
        StartPosition = FormStartPosition.CenterParent;

        _cmbMes.Items.AddRange(["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"]);
        _cmbMes.SelectedIndex = DateTime.Today.Month - 1;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        var btnGenerar = new Button { Text = "Generar" };
        var btnEmitir = new Button { Text = "Emitir listado", Width = 100 };
        btnGenerar.Click += (_, _) => Generar();
        btnEmitir.Click += (_, _) => Emitir();
        top.Controls.AddRange([new Label { Text = "Mes", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, _cmbMes,
            new Label { Text = "Año", AutoSize = true, Margin = new Padding(8, 6, 0, 0) }, _numAnio, btnGenerar, btnEmitir, _lblTotales]);

        Controls.Add(_grid);
        Controls.Add(top);
        Load += (_, _) => Generar();
    }

    private void Generar()
    {
        try
        {
            _filas = _facturas.ListadoMensual((int)_numAnio.Value, _cmbMes.SelectedIndex + 1);
            _grid.DataSource = _filas;
            _lblTotales.Text = $"Facturado ${_filas.Sum(f => f.Total):N2} · Cobrado ${_filas.Sum(f => f.Cobrado):N2} · Pendiente ${_filas.Sum(f => f.Pendiente):N2}";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void Emitir() =>
        Comprobante.AbrirListadoMensual((int)_numAnio.Value, _cmbMes.SelectedIndex + 1, _filas);
}
