using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Ventas
{
    public class RegistrarVentaForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbSucursal = null!, cmbCliente = null!, cmbPago = null!, cmbMoto = null!;
        private TextBox txtPrecio = null!, txtObs = null!;
        private Label lblStock = null!;
        private NumericUpDown nudCantidad = null!;
        private Label lblTotal = null!;

        public RegistrarVentaForm(Usuario user) { _user = user; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.LightBg };

            var card = new Panel { Width = 580, Left = 0, Top = 0, BackColor = AppTheme.White, AutoSize = true, Padding = new Padding(24) };
            card.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 230)), 0, 0, card.Width - 1, card.Height - 1);

            int y = 16;
            var title = new Label { Text = "🛒  Registrar Nueva Venta", Font = AppTheme.FontTitle, ForeColor = AppTheme.Primary, Left = 16, Top = y, AutoSize = true };
            card.Controls.Add(title); y += 40;

            card.Controls.Add(new Label { Text = "Sucursal *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbSucursal = AppTheme.MakeCombo(340); cmbSucursal.Left = 16; cmbSucursal.Top = y;
            foreach (var s in Repository.GetSucursales(true)) { cmbSucursal.Items.Add(s); if (s.Id == _user.SucursalId) cmbSucursal.SelectedItem = s; }
            if (cmbSucursal.SelectedIndex < 0 && cmbSucursal.Items.Count > 0) cmbSucursal.SelectedIndex = 0;
            card.Controls.Add(cmbSucursal); y += 36;

            card.Controls.Add(new Label { Text = "Cliente (opcional)", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbCliente = AppTheme.MakeCombo(340); cmbCliente.Left = 16; cmbCliente.Top = y;
            cmbCliente.Items.Add(new Cliente { Id = 0, Nombre = "— Sin cliente —" });
            foreach (var c in Repository.GetClientes()) cmbCliente.Items.Add(c);
            cmbCliente.SelectedIndex = 0;
            card.Controls.Add(cmbCliente); y += 36;

            // El modelo se elige del catálogo de Gestión de Motos (solo modelos activos) y trae su precio de lista.
            card.Controls.Add(new Label { Text = "Moto (marca y modelo) *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbMoto = AppTheme.MakeCombo(340); cmbMoto.Left = 16; cmbMoto.Top = y;
            cmbMoto.SelectedIndexChanged += (s, e) => MotoElegida();
            card.Controls.Add(cmbMoto);
            lblStock = new Label { Left = 364, Top = y + 4, AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted };
            card.Controls.Add(lblStock); y += 36;
            CargarMotos();

            nudCantidad = new NumericUpDown { Left = 16, Top = y + 20, Width = 100, Minimum = 1, Maximum = 999, Value = 1, Font = AppTheme.FontNormal };
            txtPrecio = AppTheme.MakeTextBox(170); txtPrecio.TextChanged += (s, e) => UpdateTotal();
            nudCantidad.ValueChanged += (s, e) => UpdateTotal();
            card.Controls.Add(new Label { Text = "Cantidad *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            card.Controls.Add(new Label { Text = "Precio unitario *", Left = 126, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            card.Controls.Add(nudCantidad); txtPrecio.Left = 126; txtPrecio.Top = y + 20; card.Controls.Add(txtPrecio); y += 58;

            card.Controls.Add(new Label { Text = "Forma de Pago *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; cmbPago = AppTheme.MakeCombo(220); cmbPago.Left = 16; cmbPago.Top = y;
            foreach (var p in new[] { "Efectivo", "Transferencia", "Crédito", "Cuenta corriente", "Cheque" }) cmbPago.Items.Add(p);
            cmbPago.SelectedIndex = 0;
            card.Controls.Add(cmbPago); y += 36;

            lblTotal = new Label { Text = "Total: $0,00", Left = 16, Top = y, AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = AppTheme.Accent };
            card.Controls.Add(lblTotal); y += 40;

            card.Controls.Add(new Label { Text = "Observaciones", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20; txtObs = AppTheme.MakeTextBox(340, true); txtObs.Left = 16; txtObs.Top = y;
            card.Controls.Add(txtObs); y += 80;

            var btnGuardar = AppTheme.MakeButton("✓  Registrar Venta", AppTheme.Accent, 200, 40);
            btnGuardar.Left = 16; btnGuardar.Top = y; btnGuardar.Click += DoSave;
            card.Controls.Add(btnGuardar);
            card.Height = y + 60;

            scroll.Controls.Add(card);
            Controls.Add(scroll);
        }

        private void CargarMotos()
        {
            cmbMoto.Items.Clear();
            foreach (var m in MotosRepository.Buscar(soloActivas: true)) cmbMoto.Items.Add(m);
            if (cmbMoto.Items.Count == 0) lblStock.Text = "No hay modelos activos: cargalos en Gestión de Motos.";
        }

        private void MotoElegida()
        {
            if (cmbMoto.SelectedItem is not Moto m) { lblStock.Text = ""; return; }
            txtPrecio.Text = m.Precio.ToString("0.##");
            lblStock.Text = m.Stock > 0 ? $"Disponibles: {m.Stock}" : "Sin stock";
            lblStock.ForeColor = m.Stock > 0 ? AppTheme.TextMuted : Color.FromArgb(220, 38, 38);
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
            if (cmbMoto.SelectedItem is not Moto moto || string.IsNullOrWhiteSpace(txtPrecio.Text) || cmbPago.SelectedItem == null)
            { MessageBox.Show("Completá los campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (nudCantidad.Value > moto.Stock)
            { MessageBox.Show($"Stock insuficiente de {moto.Descripcion}: hay {moto.Stock} y se quieren vender {nudCantidad.Value}.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!AppTheme.TryParseMonto(txtPrecio.Text, out decimal precio) || precio <= 0)
            { MessageBox.Show("El precio debe ser un número mayor a 0 (por ejemplo 285000 o 285000,50).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var suc = (Sucursal?)cmbSucursal.SelectedItem;
            var cli = (Cliente?)cmbCliente.SelectedItem;
            if (cmbPago.SelectedItem?.ToString() == "Cuenta corriente" && (cli == null || cli.Id == 0))
            { MessageBox.Show("Una venta en Cuenta corriente necesita un cliente. Elegí un cliente o cambiá la forma de pago.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int facturaId;
            try
            {
            facturaId = Repository.SaveVenta(new Venta
            {
                Fecha = DateTime.Today.ToString("yyyy-MM-dd"),
                SucursalId = suc?.Id ?? 0,
                ClienteId = cli?.Id == 0 ? null : cli?.Id,
                Marca = moto.Marca,
                Modelo = moto.Modelo,
                Cantidad = (int)nudCantidad.Value,
                PrecioUnitario = precio,
                Total = precio * (decimal)nudCantidad.Value,
                FormaPago = cmbPago.SelectedItem?.ToString() ?? "",
                Observaciones = txtObs.Text.Trim()
            });
            }
            catch (InvalidOperationException ex)
            {
                // Lo rechaza el catálogo (sin stock o modelo dado de baja mientras la pantalla estaba abierta).
                MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CargarMotos();
                return;
            }
            var factura = FacturacionRepository.Obtener(facturaId);
            MessageBox.Show($"✅  Venta registrada exitosamente.\nSe generó la factura N° {factura?.NroTexto}. Se abrirá el PDF para descargarla o imprimirla.",
                "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtPrecio.Clear(); txtObs.Clear(); nudCantidad.Value = 1;
            CargarMotos();   // refresca el stock disponible

            // La venta ya quedó guardada; si el PDF falla, se avisa y se puede volver a emitir desde Facturas.
            try { UI.Comprobantes.AbrirFacturaPorId(facturaId); }
            catch (Exception ex)
            {
                MessageBox.Show("La venta y su factura se guardaron, pero no se pudo generar el PDF: " + ex.Message +
                    "\nPodés volver a emitirlo desde Facturas > Emitir comprobante.", "PDF no generado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
