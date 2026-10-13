using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Compras
{
    public class CotizacionesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;

        public CotizacionesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 320, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "📄  Generar Solicitud de Cotización", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            var cmbSolicitud = AppTheme.MakeCombo(290);
            foreach (var s in Repository.GetSolicitudesCompra()) cmbSolicitud.Items.Add(s);
            if (cmbSolicitud.Items.Count > 0) cmbSolicitud.SelectedIndex = 0;

            var txtDesc = new TextBox { Width = 290, Height = 70, Multiline = true, Font = AppTheme.FontNormal, BorderStyle = BorderStyle.FixedSingle };

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 290; left.Controls.Add(c); y += 36; }
            Field("Solicitud de Compra *", cmbSolicitud); y += 8; Field("Descripción / Notas para proveedor", txtDesc);

            y += 44;
            var btn = AppTheme.MakeButton("📄  Generar", AppTheme.Accent, 130, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (cmbSolicitud.SelectedItem is not SolicitudCompra sc) { MessageBox.Show("Seleccioná una solicitud.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Repository.SaveSolicitudCotizacion(new SolicitudCotizacion { SolicitudCompraId = sc.Id, Fecha = DateTime.Today.ToString("yyyy-MM-dd"), Descripcion = txtDesc.Text.Trim() });
                MessageBox.Show("✅  Solicitud de cotización generada.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtDesc.Clear(); LoadGrid();
            };
            left.Controls.Add(btn); y += 50;

            // Modificar
            left.Controls.Add(new Label { Text = "── Modificar solicitud seleccionada ──", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted }); y += 24;
            var cmbEstado = AppTheme.MakeCombo(180);
            foreach (var e in new[] { "enviada", "respondida", "cancelada" }) cmbEstado.Items.Add(e);
            cmbEstado.SelectedIndex = 0;
            left.Controls.Add(new Label { Text = "Nuevo estado:", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20;
            cmbEstado.Left = 0; cmbEstado.Top = y; left.Controls.Add(cmbEstado); y += 36;
            var btnMod = AppTheme.MakeButton("✏ Modificar", AppTheme.Info, 120, 32);
            btnMod.Left = 0; btnMod.Top = y;
            btnMod.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not SolicitudCotizacion sc) { MessageBox.Show("Seleccioná una fila.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                sc.Estado = cmbEstado.SelectedItem?.ToString() ?? "enviada";
                Repository.SaveSolicitudCotizacion(sc);
                LoadGrid();
            };
            left.Controls.Add(btnMod);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("SolCompra", "Sol. Compra"); grid.Columns.Add("Desc", "Descripción"); grid.Columns.Add("Estado", "Estado");
            right.Controls.Add(grid);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var sc in Repository.GetSolicitudesCotizacion())
            {
                int i = grid.Rows.Add($"SC-{sc.Id:D3}", sc.Fecha, $"#SC-{sc.SolicitudCompraId:D3}", sc.Descripcion, sc.Estado.ToUpper());
                grid.Rows[i].Tag = sc;
            }
        }
    }
}
