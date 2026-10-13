using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Compras
{
    public class OrdenesCompraForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public OrdenesCompraForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 320, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);
            int y = 10;
            left.Controls.Add(new Label { Text = "📦  Generar Orden de Compra", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            var cmbCot = AppTheme.MakeCombo(290);
            foreach (var c in Repository.GetCotizaciones()) if (!c.Seleccionada) cmbCot.Items.Add(c);
            if (cmbCot.Items.Count > 0) cmbCot.SelectedIndex = 0;

            var txtObs = new TextBox { Width = 290, Height = 70, Multiline = true, Font = AppTheme.FontNormal, BorderStyle = BorderStyle.FixedSingle };
            var lblMonto = new Label { Text = "Monto: $0,00", AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Accent };

            cmbCot.SelectedIndexChanged += (s, e) => {
                if (cmbCot.SelectedItem is Cotizacion c) lblMonto.Text = $"Monto: {c.MontoTotal:C2}";
            };

            left.Controls.Add(new Label { Text = "Cotización *", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20;
            cmbCot.Left = 0; cmbCot.Top = y; cmbCot.Width = 290; left.Controls.Add(cmbCot); y += 36;
            lblMonto.Left = 0; lblMonto.Top = y; left.Controls.Add(lblMonto); y += 28;
            left.Controls.Add(new Label { Text = "Observaciones", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20;
            txtObs.Left = 0; txtObs.Top = y; left.Controls.Add(txtObs); y += 82;

            var info = new Label { Text = "⚠ La orden quedará pendiente de aprobación financiera.", Left = 0, Top = y, Width = 290, Height = 30, Font = AppTheme.FontSmall, ForeColor = AppTheme.Warning, AutoSize = false };
            left.Controls.Add(info); y += 36;

            var btn = AppTheme.MakeButton("📦  Generar Orden", AppTheme.Accent, 160, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (cmbCot.SelectedItem is not Cotizacion c) { MessageBox.Show("Seleccioná una cotización.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Repository.GenerarOrden(c.Id, c.MontoTotal, txtObs.Text.Trim());
                MessageBox.Show("✅  Orden generada y enviada a Finanzas.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cmbCot.Items.Clear();
                foreach (var cot in Repository.GetCotizaciones()) if (!cot.Seleccionada) cmbCot.Items.Add(cot);
                if (cmbCot.Items.Count > 0) cmbCot.SelectedIndex = 0;
                lblMonto.Text = "Monto: $0,00"; txtObs.Clear(); LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha"); grid.Columns.Add("Prov", "Proveedor");
            grid.Columns.Add("Monto", "Monto"); grid.Columns.Add("Estado", "Estado");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";
            right.Controls.Add(grid);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var o in Repository.GetOrdenesCompra())
            {
                int i = grid.Rows.Add($"OC-{o.Id:D3}", o.FechaEmision, o.ProveedorNombre, o.MontoTotal, o.Estado.ToUpper());
                var color = o.Estado switch { "aprobada" => Color.FromArgb(220, 252, 231), "rechazada" => Color.FromArgb(254, 226, 226), _ => Color.FromArgb(255, 251, 235) };
                grid.Rows[i].DefaultCellStyle.BackColor = color;
            }
        }
    }
}
