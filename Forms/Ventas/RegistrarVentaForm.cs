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
        private ComboBox cmbSucursal = null!, cmbCliente = null!, cmbPago = null!;
        private TextBox txtMarca = null!, txtModelo = null!, txtPrecio = null!, txtObs = null!;
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

            void Row2(string l1, string l2, Control c1, Control c2)
            {
                card.Controls.Add(new Label { Text = l1, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                card.Controls.Add(new Label { Text = l2, Left = 186, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                c1.Left = 16; c1.Top = y + 20; c1.Width = 160;
                c2.Left = 186; c2.Top = y + 20; c2.Width = 170;
                card.Controls.Add(c1); card.Controls.Add(c2);
            }

            txtMarca = AppTheme.MakeTextBox(); txtModelo = AppTheme.MakeTextBox();
            Row2("Marca *", "Modelo *", txtMarca, txtModelo); y += 58;

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

        private void UpdateTotal()
        {
            if (decimal.TryParse(txtPrecio.Text, out decimal p))
                lblTotal.Text = $"Total: {(p * (decimal)nudCantidad.Value):C2}";
            else
                lblTotal.Text = "Total: $0,00";
        }

        private void DoSave(object? s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModelo.Text) || string.IsNullOrWhiteSpace(txtPrecio.Text) || cmbPago.SelectedItem == null)
            { MessageBox.Show("Completá los campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
            { MessageBox.Show("El precio debe ser un número mayor a 0.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var suc = (Sucursal?)cmbSucursal.SelectedItem;
            var cli = (Cliente?)cmbCliente.SelectedItem;
            Repository.SaveVenta(new Venta
            {
                Fecha = DateTime.Today.ToString("yyyy-MM-dd"),
                SucursalId = suc?.Id ?? 0,
                ClienteId = cli?.Id == 0 ? null : cli?.Id,
                Marca = txtMarca.Text.Trim(),
                Modelo = txtModelo.Text.Trim(),
                Cantidad = (int)nudCantidad.Value,
                PrecioUnitario = precio,
                Total = precio * (decimal)nudCantidad.Value,
                FormaPago = cmbPago.SelectedItem?.ToString() ?? "",
                Observaciones = txtObs.Text.Trim()
            });
            MessageBox.Show("✅  Venta registrada exitosamente.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtMarca.Clear(); txtModelo.Clear(); txtPrecio.Clear(); txtObs.Clear(); nudCantidad.Value = 1;
        }
    }
}
