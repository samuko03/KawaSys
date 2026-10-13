using KawaSys.Desktop.Data;

namespace KawaSys.Desktop.Forms;

/// <summary>Consulta, emisión y anulación de facturas (KAWA-43/44/45/46/51/52).</summary>
public class FacturasForm : Form
{
    private readonly FacturaService _facturas = new();
    private readonly VentaService _ventas = new();
    private readonly ComboBox _cmbCliente = new() { Width = 180 };
    private readonly ComboBox _cmbSucursal = new() { Width = 150 };
    private readonly DateTimePicker _dtDesde = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker _dtHasta = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly CheckBox _chkPendientes = new() { Text = "Solo pendientes", AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
    private readonly DataGridView _grid = Grilla();
    private readonly DataGridView _gridDetalle = Grilla();
    private readonly Label _lblResumen = new() { AutoSize = true, Margin = new Padding(12, 8, 0, 0) };

    public FacturasForm()
    {
        Text = "Facturas";
        Size = new Size(1100, 640);
        StartPosition = FormStartPosition.CenterParent;

        var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(6), WrapContents = true };
        var btnBuscar = new Button { Text = "Buscar" };
        btnBuscar.Click += (_, _) => Buscar();
        filtros.Controls.AddRange([Lbl("Cliente"), _cmbCliente, Lbl("Sucursal"), _cmbSucursal,
            Lbl("Desde"), _dtDesde, Lbl("Hasta"), _dtHasta, _chkPendientes, btnBuscar]);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6) };
        var btnEmitir = new Button { Text = "Emitir comprobante", Width = 140 };
        var btnAnular = new Button { Text = "Anular factura", Width = 120 };
        var btnGenerar = new Button { Text = "Generar facturas pendientes", Width = 180 };
        btnEmitir.Click += (_, _) => Emitir();
        btnAnular.Click += (_, _) => Anular();
        btnGenerar.Click += (_, _) => GenerarPendientes();
        acciones.Controls.AddRange([btnEmitir, btnAnular, btnGenerar, _lblResumen]);

        _gridDetalle.Dock = DockStyle.Bottom;
        _gridDetalle.Height = 150;
        _grid.SelectionChanged += (_, _) => CargarDetalle();

        Controls.Add(_grid);
        Controls.Add(_gridDetalle);
        Controls.Add(acciones);
        Controls.Add(filtros);
        Load += (_, _) => { CargarCombos(); Buscar(); };
    }

    private static DataGridView Grilla() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
    };

    private static Label Lbl(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };

    private FacturaRow? Seleccion => _grid.CurrentRow?.DataBoundItem as FacturaRow;

    private void CargarCombos()
    {
        try
        {
            Opcion.Llenar(_cmbCliente, _ventas.Clientes());
            Opcion.Llenar(_cmbSucursal, _ventas.Sucursales());
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void Buscar()
    {
        try
        {
            var filas = _facturas.Listar(new FiltroFacturas(
                Opcion.Seleccionado(_cmbCliente), Opcion.Seleccionado(_cmbSucursal),
                _dtDesde.Checked ? _dtDesde.Value : null,
                _dtHasta.Checked ? _dtHasta.Value : null,
                _chkPendientes.Checked));
            _grid.DataSource = filas;
            _lblResumen.Text = $"{filas.Count} facturas · Total ${filas.Sum(f => f.Total):N2} · Pendiente ${filas.Where(f => f.Estado != "Anulada").Sum(f => f.Pendiente):N2}";
            CargarDetalle();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error de base de datos"); }
    }

    private void CargarDetalle()
    {
        _gridDetalle.DataSource = Seleccion is { } f ? _facturas.Detalle(f.IdFactura) : null;
    }

    private void Emitir()
    {
        if (Seleccion is not { } f) { MessageBox.Show("Seleccione una factura."); return; }
        Comprobante.AbrirFactura(_facturas.Obtener(f.IdFactura) ?? f, _facturas.Detalle(f.IdFactura));
    }

    private void Anular()
    {
        if (Seleccion is not { } f) { MessageBox.Show("Seleccione una factura."); return; }
        if (MessageBox.Show($"¿Anular la factura N° {f.NroFactura}? Se devolverá el stock de la venta.", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try { _facturas.Anular(f.IdFactura); Buscar(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo anular"); }
    }

    private void GenerarPendientes()
    {
        try
        {
            int n = _facturas.GenerarPendientes();
            MessageBox.Show(n == 0 ? "No hay ventas sin facturar." : $"Se generaron {n} facturas.");
            Buscar();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudieron generar las facturas"); }
    }
}
