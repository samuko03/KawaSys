using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Facturacion
{
    /// <summary>Facturas: generar, detalle, emitir y consultar con filtros (KAWA-43/44/45/46/51/52).</summary>
    public class FacturasForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbCliente = null!, cmbSucursal = null!;
        private DateTimePicker dtDesde = null!, dtHasta = null!;
        private CheckBox chkDesde = null!, chkHasta = null!, chkPendientes = null!;
        private DataGridView grid = null!, gridDetalle = null!;
        private Label lblResumen = null!;

        public FacturasForm(Usuario u) { _user = u; Build(); }

        internal static Color ColorEstado(string estado) => estado switch
        {
            "cobrada" => AppTheme.Success,
            "parcial" => AppTheme.Warning,
            "anulada" => AppTheme.TextMuted,
            _ => AppTheme.Info
        };

        internal static string Cap(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            var filter = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            int x = 10;
            filter.Controls.Add(new Label { Text = "Cliente:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbCliente = AppTheme.MakeCombo(190); cmbCliente.Left = x; cmbCliente.Top = 32;
            cmbCliente.Items.Add(new Cliente { Id = 0, Nombre = "Todos" });
            foreach (var c in Repository.GetClientes()) cmbCliente.Items.Add(c);
            cmbCliente.SelectedIndex = 0;
            filter.Controls.Add(cmbCliente); x += 205;

            filter.Controls.Add(new Label { Text = "Sucursal:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbSucursal = AppTheme.MakeCombo(170); cmbSucursal.Left = x; cmbSucursal.Top = 32;
            cmbSucursal.Items.Add(new Sucursal { Id = 0, Nombre = "Todas" });
            foreach (var s in Repository.GetSucursales(true)) cmbSucursal.Items.Add(s);
            cmbSucursal.SelectedIndex = 0;
            filter.Controls.Add(cmbSucursal); x += 185;

            chkDesde = new CheckBox { Text = "Desde:", Left = x, Top = 10, AutoSize = true, Font = AppTheme.FontBold };
            dtDesde = new DateTimePicker { Left = x, Top = 32, Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Enabled = false };
            chkDesde.CheckedChanged += (s, e) => dtDesde.Enabled = chkDesde.Checked;
            filter.Controls.AddRange(new Control[] { chkDesde, dtDesde }); x += 135;

            chkHasta = new CheckBox { Text = "Hasta:", Left = x, Top = 10, AutoSize = true, Font = AppTheme.FontBold };
            dtHasta = new DateTimePicker { Left = x, Top = 32, Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false };
            chkHasta.CheckedChanged += (s, e) => dtHasta.Enabled = chkHasta.Checked;
            filter.Controls.AddRange(new Control[] { chkHasta, dtHasta }); x += 135;

            chkPendientes = new CheckBox { Text = "Solo pendientes", Left = x, Top = 34, AutoSize = true, Font = AppTheme.FontNormal };
            filter.Controls.Add(chkPendientes); x += 135;

            var btnBuscar = AppTheme.MakeButton("🔍 Buscar", AppTheme.Accent, 100, 30);
            btnBuscar.Left = x; btnBuscar.Top = 30; btnBuscar.Click += (s, e) => LoadData();
            filter.Controls.Add(btnBuscar);

            var acciones = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 6, 12, 6) };
            acciones.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, acciones.Height - 1, acciones.Width, acciones.Height - 1);
            var btnEmitir = AppTheme.MakeButton("🖨  Emitir comprobante", AppTheme.Info, 190, 32);
            var btnGenerar = AppTheme.MakeButton("⚙  Generar facturas pendientes", AppTheme.Primary, 250, 32);
            btnEmitir.Click += (s, e) => Emitir();
            btnGenerar.Click += (s, e) => GenerarPendientes();
            lblResumen = new Label { AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Accent, Margin = new Padding(12, 8, 0, 0) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            btnEmitir.Margin = new Padding(0, 0, 8, 0); btnGenerar.Margin = new Padding(0, 0, 8, 0);
            flow.Controls.AddRange(new Control[] { btnEmitir, btnGenerar, lblResumen });
            acciones.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Nro", "N° Factura"); grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Cliente", "Cliente"); grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Total", "Total"); grid.Columns.Add("Cobrado", "Cobrado");
            grid.Columns.Add("Pend", "Pendiente"); grid.Columns.Add("Estado", "Estado");
            foreach (var c in new[] { "Total", "Cobrado", "Pend" }) grid.Columns[c].DefaultCellStyle.Format = "C2";
            grid.MultiSelect = false;
            grid.SelectionChanged += (s, e) => CargarDetalle();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Emitir(); };

            var detallePanel = new Panel { Height = 150, Dock = DockStyle.Bottom, BackColor = AppTheme.White, Padding = new Padding(0, 28, 0, 0) };
            detallePanel.Controls.Add(new Label { Text = "Detalle de la factura seleccionada", Left = 8, Top = 6, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Primary });
            gridDetalle = AppTheme.MakeGrid();
            gridDetalle.Columns.Add("Desc", "Descripción"); gridDetalle.Columns.Add("Cant", "Cant.");
            gridDetalle.Columns.Add("PU", "Precio unit."); gridDetalle.Columns.Add("Sub", "Subtotal");
            gridDetalle.Columns["PU"].DefaultCellStyle.Format = "C2"; gridDetalle.Columns["Sub"].DefaultCellStyle.Format = "C2";
            detallePanel.Controls.Add(gridDetalle);

            Controls.Add(grid);
            Controls.Add(detallePanel);
            Controls.Add(acciones);
            Controls.Add(filter);
            LoadData();
        }

        private Factura? Seleccion => grid.SelectedRows.Count == 0 ? null : grid.SelectedRows[0].Tag as Factura;

        private void LoadData()
        {
            try
            {
                int? cli = (cmbCliente.SelectedItem as Cliente) is { Id: > 0 } c ? c.Id : null;
                int? suc = (cmbSucursal.SelectedItem as Sucursal) is { Id: > 0 } s ? s.Id : null;
                var lista = FacturacionRepository.Listar(cli, suc,
                    chkDesde.Checked ? dtDesde.Value.ToString("yyyy-MM-dd") : null,
                    chkHasta.Checked ? dtHasta.Value.ToString("yyyy-MM-dd") : null,
                    chkPendientes.Checked);
                grid.Rows.Clear();
                decimal pend = 0;
                foreach (var f in lista)
                {
                    int i = grid.Rows.Add(f.NroTexto, f.Fecha, f.ClienteNombre, f.SucursalNombre, f.Total, f.Cobrado, f.Pendiente, Cap(f.Estado));
                    grid.Rows[i].Tag = f;
                    grid.Rows[i].Cells["Estado"].Style.ForeColor = ColorEstado(f.Estado);
                    grid.Rows[i].Cells["Estado"].Style.Font = AppTheme.FontBold;
                    if (f.Estado != "anulada") pend += f.Pendiente;
                }
                lblResumen.Text = $"{lista.Count} facturas · Pendiente de cobro: {pend:C2}";
                grid.ClearSelection();
                if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;
                CargarDetalle();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void CargarDetalle()
        {
            gridDetalle.Rows.Clear();
            if (Seleccion is not Factura f) return;
            foreach (var l in FacturacionRepository.Detalle(f.Id))
                gridDetalle.Rows.Add(l.Descripcion, l.Cantidad, l.PrecioUnitario, l.SubTotal);
        }

        private void Emitir()
        {
            if (Seleccion is not Factura f) { MessageBox.Show("Seleccioná una factura de la lista.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            Comprobantes.AbrirFactura(FacturacionRepository.Obtener(f.Id) ?? f, FacturacionRepository.Detalle(f.Id));
        }

        private void GenerarPendientes()
        {
            try
            {
                int n = FacturacionRepository.GenerarPendientes();
                MessageBox.Show(n == 0 ? "Todas las ventas ya tienen factura." : $"Se generaron {n} facturas.", "KawaSys",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
