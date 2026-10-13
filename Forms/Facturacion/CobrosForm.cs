using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Facturacion
{
    /// <summary>Pagos pendientes, registro de cobros con medio de pago y recibos (KAWA-47/48/49/50).</summary>
    public class CobrosForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbCliente = null!, cmbMedio = null!;
        private DataGridView grid = null!, gridCobros = null!;
        private NumericUpDown nudMonto = null!;
        private TextBox txtObs = null!;
        private Label lblResumen = null!, lblSeleccion = null!;

        public CobrosForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            var filter = new Panel { Height = 70, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);
            filter.Controls.Add(new Label { Text = "Cliente:", Left = 10, Top = 8, AutoSize = true, Font = AppTheme.FontBold });
            cmbCliente = AppTheme.MakeCombo(240); cmbCliente.Left = 10; cmbCliente.Top = 30;
            cmbCliente.Items.Add(new Cliente { Id = 0, Nombre = "Todos" });
            foreach (var c in Repository.GetClientes()) cmbCliente.Items.Add(c);
            cmbCliente.SelectedIndex = 0;
            filter.Controls.Add(cmbCliente);
            var btnBuscar = AppTheme.MakeButton("🔍 Pagos pendientes", AppTheme.Accent, 170, 30);
            btnBuscar.Left = 265; btnBuscar.Top = 28; btnBuscar.Click += (s, e) => LoadData();
            filter.Controls.Add(btnBuscar);
            lblResumen = new Label { Left = 450, Top = 34, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Accent };
            filter.Controls.Add(lblResumen);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Nro", "N° Factura"); grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Cliente", "Cliente"); grid.Columns.Add("Total", "Total");
            grid.Columns.Add("Cobrado", "Cobrado"); grid.Columns.Add("Pend", "Pendiente"); grid.Columns.Add("Estado", "Estado");
            foreach (var c in new[] { "Total", "Cobrado", "Pend" }) grid.Columns[c].DefaultCellStyle.Format = "C2";
            grid.MultiSelect = false;
            grid.SelectionChanged += (s, e) => SeleccionCambiada();

            // Panel inferior: cobros de la factura (izquierda) + formulario de cobro (derecha)
            var bottom = new Panel { Height = 230, Dock = DockStyle.Bottom, BackColor = AppTheme.White };
            bottom.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, 0, bottom.Width, 0);

            var form = new Panel { Width = 340, Dock = DockStyle.Right, BackColor = AppTheme.White, Padding = new Padding(14) };
            lblSeleccion = new Label { Text = "Seleccioná una factura pendiente", Left = 14, Top = 10, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Primary };
            form.Controls.Add(lblSeleccion);
            form.Controls.Add(new Label { Text = "Monto a cobrar", Left = 14, Top = 38, AutoSize = true, Font = AppTheme.FontBold });
            nudMonto = new NumericUpDown { Left = 14, Top = 58, Width = 150, DecimalPlaces = 2, Maximum = 1_000_000_000, ThousandsSeparator = true, Font = AppTheme.FontNormal };
            form.Controls.Add(nudMonto);
            form.Controls.Add(new Label { Text = "Medio de pago", Left = 180, Top = 38, AutoSize = true, Font = AppTheme.FontBold });
            cmbMedio = AppTheme.MakeCombo(140); cmbMedio.Left = 180; cmbMedio.Top = 58;
            foreach (var m in FacturacionRepository.MediosDePago) cmbMedio.Items.Add(m);
            cmbMedio.SelectedIndex = 0;
            form.Controls.Add(cmbMedio);
            form.Controls.Add(new Label { Text = "Observaciones", Left = 14, Top = 92, AutoSize = true, Font = AppTheme.FontBold });
            txtObs = AppTheme.MakeTextBox(306); txtObs.Left = 14; txtObs.Top = 112;
            form.Controls.Add(txtObs);
            var btnCobrar = AppTheme.MakeButton("✓  Registrar cobro", AppTheme.Success, 306, 38);
            btnCobrar.Left = 14; btnCobrar.Top = 150; btnCobrar.Click += (s, e) => Cobrar();
            form.Controls.Add(btnCobrar);

            var izq = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 0) };
            izq.Controls.Add(new Label { Text = "Cobros registrados de la factura seleccionada", Left = 8, Top = 6, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Primary });
            gridCobros = AppTheme.MakeGrid();
            gridCobros.Columns.Add("Recibo", "Recibo N°"); gridCobros.Columns.Add("Fecha", "Fecha");
            gridCobros.Columns.Add("Monto", "Monto"); gridCobros.Columns.Add("Medio", "Medio de pago");
            gridCobros.Columns["Monto"].DefaultCellStyle.Format = "C2";
            izq.Controls.Add(gridCobros);
            var btnRecibo = AppTheme.MakeButton("🧾  Emitir recibo", AppTheme.Info, 150, 30);
            btnRecibo.Dock = DockStyle.Bottom; btnRecibo.Click += (s, e) => EmitirReciboSeleccionado();
            izq.Controls.Add(btnRecibo);

            bottom.Controls.Add(izq);
            bottom.Controls.Add(form);

            Controls.Add(grid);
            Controls.Add(bottom);
            Controls.Add(filter);
            LoadData();
        }

        private Factura? Seleccion => grid.SelectedRows.Count == 0 ? null : grid.SelectedRows[0].Tag as Factura;

        private void LoadData()
        {
            try
            {
                int? cli = (cmbCliente.SelectedItem as Cliente) is { Id: > 0 } c ? c.Id : null;
                var lista = FacturacionRepository.Listar(clienteId: cli, soloPendientes: true);
                grid.Rows.Clear();
                foreach (var f in lista)
                {
                    int i = grid.Rows.Add(f.NroTexto, f.Fecha, f.ClienteNombre, f.Total, f.Cobrado, f.Pendiente, FacturasForm.Cap(f.Estado));
                    grid.Rows[i].Tag = f;
                    grid.Rows[i].Cells["Estado"].Style.ForeColor = FacturasForm.ColorEstado(f.Estado);
                }
                lblResumen.Text = $"{lista.Count} facturas pendientes · Total a cobrar: {lista.Sum(f => f.Pendiente):C2}";
                grid.ClearSelection();
                if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;
                SeleccionCambiada();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SeleccionCambiada()
        {
            gridCobros.Rows.Clear();
            if (Seleccion is not Factura f)
            {
                lblSeleccion.Text = "Seleccioná una factura pendiente";
                nudMonto.Value = 0;
                return;
            }
            lblSeleccion.Text = $"Factura N° {f.NroTexto} — pendiente {f.Pendiente:C2}";
            nudMonto.Value = Math.Min(Math.Max(Math.Round(f.Pendiente, 2), 0), nudMonto.Maximum);
            foreach (var c in FacturacionRepository.Cobros(f.Id))
            {
                int i = gridCobros.Rows.Add(c.NroRecibo, c.Fecha, c.Monto, c.MedioPago);
                gridCobros.Rows[i].Tag = c;
            }
        }

        private void Cobrar()
        {
            if (Seleccion is not Factura f) { MessageBox.Show("Seleccioná una factura pendiente.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                int cobroId = FacturacionRepository.RegistrarCobro(f.Id, nudMonto.Value, cmbMedio.SelectedItem?.ToString() ?? "", txtObs.Text.Trim());
                txtObs.Clear();
                LoadData();
                if (MessageBox.Show("✅  Cobro registrado. ¿Emitir el recibo?", "KawaSys", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    AbrirRecibo(cobroId, f.Id);
            }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "No se pudo registrar el cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void EmitirReciboSeleccionado()
        {
            if (gridCobros.SelectedRows.Count == 0 || gridCobros.SelectedRows[0].Tag is not Cobro c)
            { MessageBox.Show("Seleccioná un cobro de la lista.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            AbrirRecibo(c.Id, c.FacturaId);
        }

        private static void AbrirRecibo(int cobroId, int facturaId)
        {
            var cobro = FacturacionRepository.Cobros(facturaId).First(c => c.Id == cobroId);
            var factura = FacturacionRepository.Obtener(facturaId)!;
            Comprobantes.AbrirRecibo(cobro, factura);
        }
    }
}
