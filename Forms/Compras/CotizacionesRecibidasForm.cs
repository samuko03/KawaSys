using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Compras
{
    public class CotizacionesRecibidasForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public CotizacionesRecibidasForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 320, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);
            int y = 10;
            left.Controls.Add(new Label { Text = "📥  Registrar Cotización Recibida", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            var cmbSolCot = AppTheme.MakeCombo(290);
            foreach (var sc in Repository.GetSolicitudesCotizacion()) cmbSolCot.Items.Add(sc);
            if (cmbSolCot.Items.Count > 0) cmbSolCot.SelectedIndex = 0;

            var cmbProv = AppTheme.MakeCombo(290);
            foreach (var p in Repository.GetProveedores()) cmbProv.Items.Add(p);
            if (cmbProv.Items.Count > 0) cmbProv.SelectedIndex = 0;

            var txtMonto = AppTheme.MakeTextBox(290);
            var txtCond = AppTheme.MakeTextBox(290);
            var txtPlazo = AppTheme.MakeTextBox(290);

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 290; left.Controls.Add(c); y += 36; }
            Field("Solicitud de Cotización *", cmbSolCot);
            Field("Proveedor *", cmbProv);
            Field("Monto Total $ *", txtMonto);
            Field("Condiciones de pago", txtCond);
            Field("Plazo de entrega", txtPlazo);

            var btn = AppTheme.MakeButton("✓  Registrar", AppTheme.Accent, 130, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (cmbSolCot.SelectedItem is not SolicitudCotizacion sc || cmbProv.SelectedItem is not Proveedor prov || !decimal.TryParse(txtMonto.Text, out decimal m))
                { MessageBox.Show("Completá los campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Repository.SaveCotizacion(new Cotizacion { SolicitudCotizacionId = sc.Id, ProveedorId = prov.Id, Fecha = DateTime.Today.ToString("yyyy-MM-dd"), MontoTotal = m, Condiciones = txtCond.Text.Trim(), PlazoEntrega = txtPlazo.Text.Trim() });
                MessageBox.Show("✅  Cotización registrada.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMonto.Clear(); txtCond.Clear(); txtPlazo.Clear(); LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha"); grid.Columns.Add("Prov", "Proveedor");
            grid.Columns.Add("Monto", "Monto"); grid.Columns.Add("Plazo", "Plazo"); grid.Columns.Add("Estado", "Estado");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";
            right.Controls.Add(grid);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var c in Repository.GetCotizaciones())
                grid.Rows.Add($"COT-{c.Id:D3}", c.Fecha, c.ProveedorNombre, c.MontoTotal, c.PlazoEntrega, c.Seleccionada ? "Seleccionada ✓" : "Disponible");
        }
    }
}
