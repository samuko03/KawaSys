using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms
{
    public class DashboardPanel : Form
    {
        private readonly Usuario _user;
        public DashboardPanel(Usuario user) { _user = user; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = AppTheme.LightBg, Padding = new Padding(0, 0, 0, 20)
            };

            // Title
            flow.Controls.Add(new Label
            {
                Text = $"Bienvenido, {_user.Nombre} 👋",
                Font = AppTheme.FontTitle, ForeColor = AppTheme.Primary,
                Width = 900, Padding = new Padding(0, 10, 0, 6), AutoSize = false
            });

            var (ventas, faltantes, ordenesPend, opPend, proveedores, sucursales) = Repository.GetStats();

            // Stats row
            var statsFlow = new FlowLayoutPanel
            {
                Width = 1200, Height = 100, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = AppTheme.LightBg
            };

            void AddStat(string lbl, string val, Color color)
            {
                var p = new Panel { Width = 170, Height = 82, BackColor = AppTheme.White, Margin = new Padding(0, 0, 10, 0) };
                p.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 230)), 0, 0, p.Width - 1, p.Height - 1);
                var numLbl = new Label { Text = val, Left = 12, Top = 10, AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = color };
                var nameLbl = new Label { Text = lbl, Left = 12, Top = 48, AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted };
                var bar = new Panel { Left = 0, Top = 0, Width = 4, Height = 82, BackColor = color };
                p.Controls.AddRange(new Control[] { bar, numLbl, nameLbl });
                statsFlow.Controls.Add(p);
            }

            AddStat("Ventas totales", ventas.ToString(), AppTheme.Accent);
            AddStat("Faltantes pend.", faltantes.ToString(), AppTheme.Warning);
            AddStat("Órdenes pend.", ordenesPend.ToString(), AppTheme.Info);
            AddStat("Op. pend. aprov.", opPend.ToString(), Color.FromArgb(139, 92, 246));
            AddStat("Proveedores", proveedores.ToString(), AppTheme.Success);
            AddStat("Sucursales", sucursales.ToString(), Color.FromArgb(20, 184, 166));
            flow.Controls.Add(statsFlow);

            // Últimas ventas
            flow.Controls.Add(new Label
            {
                Text = "Últimas ventas registradas",
                Font = AppTheme.FontBold, ForeColor = AppTheme.TextDark,
                Width = 900, Padding = new Padding(0, 14, 0, 4), AutoSize = false
            });

            var grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Moto", "Moto");
            grid.Columns.Add("Total", "Total");
            grid.Columns.Add("Pago", "Forma de Pago");
            grid.Columns["Id"].FillWeight = 40;
            grid.Columns["Total"].DefaultCellStyle.Format = "C2";

            foreach (var v in Repository.GetVentas())
                grid.Rows.Add($"#{v.Id}", v.Fecha, v.SucursalNombre, $"{v.Marca} {v.Modelo}", v.Total, v.FormaPago);

            var gridContainer = new Panel { Width = 880, Height = 240, BackColor = AppTheme.White };
            grid.Dock = DockStyle.Fill;
            gridContainer.Controls.Add(grid);
            flow.Controls.Add(gridContainer);

            Controls.Add(flow);
        }
    }
}
