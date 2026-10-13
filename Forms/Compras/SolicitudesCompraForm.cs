using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Compras
{
    public class SolicitudesCompraForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;

        public SolicitudesCompraForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 340, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "🛍  Nueva Solicitud de Compra", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            left.Controls.Add(new Label { Text = "Descripción / Justificación *", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20;
            var txtDesc = new TextBox { Left = 0, Top = y, Width = 300, Height = 60, Multiline = true, Font = AppTheme.FontNormal, BorderStyle = BorderStyle.FixedSingle };
            left.Controls.Add(txtDesc); y += 72;

            // Items dinámicos
            left.Controls.Add(new Label { Text = "Ítems a solicitar:", Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 24;
            var itemsPanel = new Panel { Left = 0, Top = y, Width = 300, Height = 160, BackColor = Color.FromArgb(245, 247, 250), AutoScroll = true };
            left.Controls.Add(itemsPanel);
            int iy = 4;

            void AddItemRow()
            {
                var marca = new TextBox { Left = 4, Top = iy, Width = 90, Height = 24, Font = AppTheme.FontSmall, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Marca" };
                var modelo = new TextBox { Left = 98, Top = iy, Width = 110, Height = 24, Font = AppTheme.FontSmall, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Modelo" };
                var cant = new NumericUpDown { Left = 212, Top = iy, Width = 60, Height = 24, Minimum = 1, Maximum = 999, Value = 1, Font = AppTheme.FontSmall };
                itemsPanel.Controls.AddRange(new Control[] { marca, modelo, cant });
                iy += 28;
                itemsPanel.AutoScrollMinSize = new System.Drawing.Size(300, iy + 10);
            }
            AddItemRow();

            y += 166;
            var btnAgregar = AppTheme.MakeButton("+ Agregar ítem", AppTheme.Info, 130, 28);
            btnAgregar.Left = 0; btnAgregar.Top = y;
            btnAgregar.Click += (s, e) => AddItemRow();
            left.Controls.Add(btnAgregar); y += 38;

            var btn = AppTheme.MakeButton("✓  Registrar", AppTheme.Accent, 130, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtDesc.Text)) { MessageBox.Show("Ingresá una descripción.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                int solId = Repository.SaveSolicitudCompra(new SolicitudCompra { Fecha = DateTime.Today.ToString("yyyy-MM-dd"), Descripcion = txtDesc.Text.Trim() });
                MessageBox.Show($"✅  Solicitud #SC-{solId:D3} registrada.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtDesc.Clear(); LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Desc", "Descripción"); grid.Columns.Add("Estado", "Estado");
            right.Controls.Add(grid);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var s in Repository.GetSolicitudesCompra())
                grid.Rows.Add($"SC-{s.Id:D3}", s.Fecha, s.Descripcion, s.Estado.ToUpper());
        }
    }
}
