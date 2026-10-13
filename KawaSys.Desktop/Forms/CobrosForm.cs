using KawaSys.Desktop.Data;

namespace KawaSys.Desktop.Forms;

/// <summary>Pagos pendientes, registro de cobros, medio de pago y recibos (KAWA-47/48/49/50).</summary>
public class CobrosForm : Form
{
    private readonly FacturaService _facturas = new();
    private readonly VentaService _ventas = new();
    private readonly ComboBox _cmbCliente = new() { Width = 200 };
    private readonly DataGridView _grid = Grilla();
    private readonly DataGridView _gridCobros = Grilla();
    private readonly NumericUpDown _numMonto = new() { Maximum = 1_000_000_000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 120 };
    private readonly ComboBox _cmbMedio = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly Label _lblTotal = new() { AutoSize = true, Margin = new Padding(12, 8, 0, 0) };

    public CobrosForm()
    {
        Text = "Cobros y pagos pendientes";
        Size = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;

        var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        var btnBuscar = new Button { Text = "Buscar pendientes" };
        btnBuscar.Click += (_, _) => Cargar();
        filtros.Controls.AddRange([Lbl("Cliente"), _cmbCliente, btnBuscar, _lblTotal]);

        var cobro = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6) };
        var btnCobrar = new Button { Text = "Registrar cobro", Width = 120 };
        var btnRecibo = new Button { Text = "Emitir recibo", Width = 110 };
        btnCobrar.Click += (_, _) => Cobrar();
        btnRecibo.Click += (_, _) => EmitirRecibo();
        _cmbMedio.Items.AddRange(FacturaService.MediosDePago);
        _cmbMedio.SelectedIndex = 0;
        cobro.Controls.AddRange([Lbl("Monto"), _numMonto, Lbl("Medio de pago"), _cmbMedio, btnCobrar, btnRecibo]);

        _gridCobros.Dock = DockStyle.Bottom;
        _gridCobros.Height = 150;
        _grid.SelectionChanged += (_, _) => SeleccionCambiada();

        Controls.Add(_grid);
        Controls.Add(_gridCobros);
        Controls.Add(cobro);
        Controls.Add(filtros);
        Load += (_, _) =>
        {
            try { Opcion.Llenar(_cmbCliente, _ventas.Clientes()); Cargar(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
        };
    }

    private static DataGridView Grilla() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
    };

    private static Label Lbl(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };

    private FacturaRow? Seleccion => _grid.CurrentRow?.DataBoundItem as FacturaRow;

    private void Cargar()
    {
        try
        {
            var filas = _facturas.Listar(new FiltroFacturas(IdCliente: Opcion.Seleccionado(_cmbCliente), SoloPendientes: true));
            _grid.DataSource = filas;
            _lblTotal.Text = $"{filas.Count} facturas pendientes · Total a cobrar ${filas.Sum(f => f.Pendiente):N2}";
            SeleccionCambiada();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void SeleccionCambiada()
    {
        if (Seleccion is { } f)
        {
            _numMonto.Value = Math.Min(f.Pendiente, _numMonto.Maximum);
            _gridCobros.DataSource = _facturas.Cobros(f.IdFactura);
        }
        else _gridCobros.DataSource = null;
    }

    private void Cobrar()
    {
        if (Seleccion is not { } f) { MessageBox.Show("Seleccione una factura pendiente."); return; }
        try
        {
            int idCobro = _facturas.RegistrarCobro(f.IdFactura, _numMonto.Value, (string)_cmbMedio.SelectedItem!);
            Cargar();
            if (MessageBox.Show("Cobro registrado. ¿Emitir el recibo?", "Cobro", MessageBoxButtons.YesNo) == DialogResult.Yes)
                AbrirRecibo(idCobro, f.IdFactura);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo registrar el cobro"); }
    }

    private void EmitirRecibo()
    {
        if (_gridCobros.CurrentRow?.DataBoundItem is not CobroRow c) { MessageBox.Show("Seleccione un cobro de la lista inferior."); return; }
        AbrirRecibo(c.IdCobro, c.IdFactura);
    }

    private void AbrirRecibo(int idCobro, int idFactura)
    {
        var cobro = _facturas.Cobros(idFactura).First(c => c.IdCobro == idCobro);
        var factura = _facturas.Obtener(idFactura)!;
        Comprobante.AbrirRecibo(cobro, factura);
    }
}
