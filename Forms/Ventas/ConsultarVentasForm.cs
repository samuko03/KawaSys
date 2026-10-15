using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Ventas
{
    public class ConsultarVentasForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbSucursal = null!;
        private DateTimePicker dtDesde = null!, dtHasta = null!;
        private DataGridView grid = null!;
        private Label lblTotal = null!, lblNota = null!;
        private Button btnEditar = null!, btnEliminar = null!;

        public ConsultarVentasForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var filter = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            int x = 10;
            filter.Controls.Add(new Label { Text = "Sucursal:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbSucursal = AppTheme.MakeCombo(200); cmbSucursal.Left = x; cmbSucursal.Top = 32;
            cmbSucursal.Items.Add(new Sucursal { Id = 0, Nombre = "Todas" });
            foreach (var s in Repository.GetSucursales(true)) cmbSucursal.Items.Add(s);
            if (_user.Rol == "ventas") { foreach (Sucursal s in cmbSucursal.Items) if (s.Id == _user.SucursalId) { cmbSucursal.SelectedItem = s; break; } }
            if (cmbSucursal.SelectedIndex < 0) cmbSucursal.SelectedIndex = 0;
            filter.Controls.Add(cmbSucursal); x += 220;

            filter.Controls.Add(new Label { Text = "Desde:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            dtDesde = new DateTimePicker { Left = x, Top = 32, Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), ShowCheckBox = true, Checked = false };
            filter.Controls.Add(dtDesde); x += 160;

            filter.Controls.Add(new Label { Text = "Hasta:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            dtHasta = new DateTimePicker { Left = x, Top = 32, Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today, ShowCheckBox = true, Checked = false };
            filter.Controls.Add(dtHasta); x += 160;

            var btnBuscar = AppTheme.MakeButton("🔍 Buscar", AppTheme.Accent, 110, 30);
            btnBuscar.Left = x; btnBuscar.Top = 30; btnBuscar.Click += (s, e) => LoadData();
            filter.Controls.Add(btnBuscar);

            lblTotal = new Label { Left = x + 120, Top = 34, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Accent };
            filter.Controls.Add(lblTotal);

            // Barra de acciones
            var acciones = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 6, 12, 6) };
            acciones.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, acciones.Height - 1, acciones.Width, acciones.Height - 1);
            btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            btnEliminar = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            lblNota = new Label { AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted, Margin = new Padding(12, 9, 0, 0) };
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Click += (s, e) => EditarSeleccionada();
            btnEliminar.Click += (s, e) => EliminarSeleccionada();
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnEditar); flow.Controls.Add(btnEliminar); flow.Controls.Add(lblNota);
            acciones.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Suc", "Sucursal"); grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Marca", "Marca"); grid.Columns.Add("Modelo", "Modelo");
            grid.Columns.Add("Cant", "Cant."); grid.Columns.Add("Total", "Total");
            grid.Columns.Add("Pago", "Forma de Pago");
            grid.Columns.Add("Factura", "Factura");
            grid.Columns["Id"].FillWeight = 35; grid.Columns["Cant"].FillWeight = 40;
            grid.Columns["Total"].DefaultCellStyle.Format = "C2";
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditarSeleccionada(); };
            grid.SelectionChanged += (s, e) => ActualizarAcciones();

            Controls.Add(grid);
            Controls.Add(acciones);
            Controls.Add(filter);
            LoadData();

            // El filtro se aplica al instante, sin tener que pulsar Buscar.
            cmbSucursal.SelectedIndexChanged += (s, e) => LoadData();
            dtDesde.ValueChanged += (s, e) => LoadData();
            dtHasta.ValueChanged += (s, e) => LoadData();
        }

        private Venta? GetSeleccionada()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as Venta;
        }

        private void EditarSeleccionada()
        {
            if (GetSeleccionada() is not Venta v)
            {
                MessageBox.Show("Seleccioná una venta de la lista.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (v.NroFactura != null) return;   // el doble clic no abre la edición de una venta facturada
            using var dlg = new EditarVentaForm(v);
            if (dlg.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void EliminarSeleccionada()
        {
            if (GetSeleccionada() is not Venta v)
            {
                MessageBox.Show("Seleccioná una venta de la lista.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var r = MessageBox.Show($"¿Eliminar la venta #{v.Id} ({v.Marca} {v.Modelo} — {v.Total:C2})?\n\nEsta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                try { Repository.DeleteVenta(v.Id); }
                catch (InvalidOperationException ex)
                { MessageBox.Show(ex.Message, "No se puede eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                LoadData();
            }
        }

        private void LoadData()
        {
            var suc = (Sucursal?)cmbSucursal.SelectedItem;
            // Las fechas son opcionales: sin tildar la casilla no se filtra por ese extremo.
            var ventas = Repository.GetVentas(suc?.Id == 0 ? null : suc?.Id,
                dtDesde.Checked ? dtDesde.Value.ToString("yyyy-MM-dd") : null,
                dtHasta.Checked ? dtHasta.Value.ToString("yyyy-MM-dd") : null);
            grid.Rows.Clear();
            decimal total = 0;
            foreach (var v in ventas)
            {
                int i = grid.Rows.Add($"#{v.Id}", v.Fecha, v.SucursalNombre, v.ClienteNombre, v.Marca, v.Modelo, v.Cantidad, v.Total, v.FormaPago,
                    v.NroFactura.HasValue ? $"N° {v.NroFactura:D8}" : "Sin facturar");
                grid.Rows[i].Tag = v;
                grid.Rows[i].Cells["Factura"].Style.ForeColor = v.NroFactura.HasValue ? AppTheme.Success : AppTheme.Warning;
                total += v.Total;
            }
            lblTotal.Text = $"Total: {total:C2}  ({ventas.Count} registros)";
            ActualizarAcciones();
        }

        // Una venta facturada ya fue efectuada: no se modifica ni se elimina (solo las "Sin facturar").
        private void ActualizarAcciones()
        {
            if (btnEditar == null || btnEliminar == null) return;
            var v = GetSeleccionada();
            bool facturada = v?.NroFactura != null;
            btnEditar.Enabled = btnEliminar.Enabled = !facturada;
            btnEditar.BackColor = facturada ? Color.FromArgb(170, 175, 190) : AppTheme.Info;
            btnEliminar.BackColor = facturada ? Color.FromArgb(170, 175, 190) : Color.FromArgb(220, 38, 38);
            lblNota.Text = facturada
                ? $"🔒  Venta ya facturada (N° {v!.NroFactura:D8}): no se puede modificar ni eliminar."
                : "";
        }
    }

    // ── Editar venta ──────────────────────────────────────────────────────────
    public class EditarVentaForm : Form
    {
        private readonly Venta _v;
        private ComboBox cmbSucursal = null!, cmbCliente = null!, cmbPago = null!;
        private TextBox txtMarca = null!, txtModelo = null!, txtPrecio = null!, txtObs = null!;
        private NumericUpDown nudCantidad = null!;
        private Label lblTotal = null!;

        public EditarVentaForm(Venta v) { _v = v; Build(); }

        private void Build()
        {
            Text = $"Modificar Venta #{_v.Id}";
            Size = new Size(560, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppTheme.White;
            AutoScroll = true;

            int y = 16;
            Controls.Add(new Label { Text = $"🛒  Modificar Venta #{_v.Id}", Font = AppTheme.FontTitle, ForeColor = AppTheme.Primary, Left = 16, Top = y, AutoSize = true });
            y += 44;

            Controls.Add(new Label { Text = "Sucursal *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbSucursal = AppTheme.MakeCombo(340); cmbSucursal.Left = 16; cmbSucursal.Top = y;
            foreach (var s in Repository.GetSucursales(true)) { cmbSucursal.Items.Add(s); if (s.Id == _v.SucursalId) cmbSucursal.SelectedItem = s; }
            if (cmbSucursal.SelectedIndex < 0 && cmbSucursal.Items.Count > 0) cmbSucursal.SelectedIndex = 0;
            Controls.Add(cmbSucursal); y += 36;

            Controls.Add(new Label { Text = "Cliente (opcional)", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbCliente = AppTheme.MakeCombo(340); cmbCliente.Left = 16; cmbCliente.Top = y;
            cmbCliente.Items.Add(new Cliente { Id = 0, Nombre = "— Sin cliente —" });
            foreach (var c in Repository.GetClientes()) { cmbCliente.Items.Add(c); if (c.Id == _v.ClienteId) cmbCliente.SelectedItem = c; }
            if (cmbCliente.SelectedIndex < 0) cmbCliente.SelectedIndex = 0;
            Controls.Add(cmbCliente); y += 36;

            Controls.Add(new Label { Text = "Marca", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Modelo *", Left = 196, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20;
            txtMarca = AppTheme.MakeTextBox(160); txtMarca.Left = 16; txtMarca.Top = y; txtMarca.Text = _v.Marca; Controls.Add(txtMarca);
            txtModelo = AppTheme.MakeTextBox(170); txtModelo.Left = 196; txtModelo.Top = y; txtModelo.Text = _v.Modelo; Controls.Add(txtModelo);
            y += 44;

            Controls.Add(new Label { Text = "Cantidad *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Precio unitario *", Left = 126, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20;
            nudCantidad = new NumericUpDown { Left = 16, Top = y, Width = 100, Minimum = 1, Maximum = 999, Value = _v.Cantidad, Font = AppTheme.FontNormal };
            nudCantidad.ValueChanged += (s, e) => UpdateTotal();
            Controls.Add(nudCantidad);
            txtPrecio = AppTheme.MakeTextBox(170); txtPrecio.Left = 126; txtPrecio.Top = y; txtPrecio.Text = _v.PrecioUnitario.ToString("0.##");
            txtPrecio.TextChanged += (s, e) => UpdateTotal();
            Controls.Add(txtPrecio); y += 44;

            Controls.Add(new Label { Text = "Forma de Pago *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbPago = AppTheme.MakeCombo(220); cmbPago.Left = 16; cmbPago.Top = y;
            foreach (var p in new[] { "Efectivo", "Transferencia", "Crédito", "Cuenta corriente", "Cheque" }) cmbPago.Items.Add(p);
            cmbPago.SelectedItem = _v.FormaPago;
            if (cmbPago.SelectedIndex < 0) cmbPago.SelectedIndex = 0;
            Controls.Add(cmbPago); y += 36;

            lblTotal = new Label { Left = 16, Top = y, AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = AppTheme.Accent };
            Controls.Add(lblTotal); y += 40;

            Controls.Add(new Label { Text = "Observaciones", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; txtObs = AppTheme.MakeTextBox(340, true); txtObs.Left = 16; txtObs.Top = y; txtObs.Text = _v.Observaciones;
            Controls.Add(txtObs); y += 90;

            var btnGuardar = AppTheme.MakeButton("✓  Guardar Cambios", AppTheme.Accent, 200, 40);
            btnGuardar.Left = 16; btnGuardar.Top = y; btnGuardar.Click += DoSave;
            Controls.Add(btnGuardar);
            ClientSize = new Size(ClientSize.Width, btnGuardar.Bottom + 24);   // que el botón Guardar quede siempre visible

            UpdateTotal();
        }

        private void UpdateTotal()
        {
            if (AppTheme.TryParseMonto(txtPrecio.Text, out decimal p))
                lblTotal.Text = $"Total: {(p * (decimal)nudCantidad.Value):C2}";
            else
                lblTotal.Text = "Total: $0,00";
        }

        private void DoSave(object? s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModelo.Text) || string.IsNullOrWhiteSpace(txtPrecio.Text) || cmbPago.SelectedItem == null)
            { MessageBox.Show("Completá los campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!AppTheme.TryParseMonto(txtPrecio.Text, out decimal precio) || precio <= 0)
            { MessageBox.Show("El precio debe ser un número mayor a 0 (por ejemplo 285000 o 285000,50).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var suc = (Sucursal?)cmbSucursal.SelectedItem;
            var cli = (Cliente?)cmbCliente.SelectedItem;
            if (cmbPago.SelectedItem?.ToString() == "Cuenta corriente" && (cli == null || cli.Id == 0))
            { MessageBox.Show("Una venta en Cuenta corriente necesita un cliente. Elegí un cliente o cambiá la forma de pago.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { Repository.UpdateVenta(new Venta
            {
                Id = _v.Id,
                Fecha = _v.Fecha,
                SucursalId = suc?.Id ?? 0,
                ClienteId = cli?.Id == 0 ? null : cli?.Id,
                Marca = txtMarca.Text.Trim(),
                Modelo = txtModelo.Text.Trim(),
                Cantidad = (int)nudCantidad.Value,
                PrecioUnitario = precio,
                Total = precio * (decimal)nudCantidad.Value,
                FormaPago = cmbPago.SelectedItem?.ToString() ?? "",
                Observaciones = txtObs.Text.Trim()
            }); }
            catch (InvalidOperationException ex)
            { MessageBox.Show(ex.Message, "No se puede modificar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
