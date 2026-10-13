using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Proveedores
{
    public class BuscarProveedorForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        private TextBox txtMarca = null!, txtModelo = null!;
        private ComboBox cmbTipo = null!;

        public BuscarProveedorForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var filter = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            int x = 10;
            filter.Controls.Add(new Label { Text = "Marca:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            txtMarca = AppTheme.MakeTextBox(140); txtMarca.Left = x; txtMarca.Top = 32; filter.Controls.Add(txtMarca); x += 160;
            filter.Controls.Add(new Label { Text = "Modelo:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            txtModelo = AppTheme.MakeTextBox(140); txtModelo.Left = x; txtModelo.Top = 32; filter.Controls.Add(txtModelo); x += 160;
            filter.Controls.Add(new Label { Text = "Tipo:", Left = x, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbTipo = AppTheme.MakeCombo(130); cmbTipo.Items.AddRange(new object[] { "Todos", "fijo", "alternativo" }); cmbTipo.SelectedIndex = 0;
            cmbTipo.Left = x; cmbTipo.Top = 32; filter.Controls.Add(cmbTipo); x += 150;
            var btn = AppTheme.MakeButton("🔍  Buscar", AppTheme.Accent, 100, 30); btn.Left = x; btn.Top = 30; btn.Click += (s, e) => LoadGrid(); filter.Controls.Add(btn);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("RS", "Razón Social");
            grid.Columns.Add("Tipo", "Tipo"); grid.Columns.Add("Marcas", "Marcas");
            grid.Columns.Add("Modelos", "Modelos"); grid.Columns.Add("Tel", "Teléfono");
            Controls.Add(grid);
            Controls.Add(filter);
            LoadGrid();
        }

        private void LoadGrid()
        {
            string? tipo = cmbTipo.SelectedItem?.ToString() == "Todos" ? null : cmbTipo.SelectedItem?.ToString();
            string? marca = string.IsNullOrWhiteSpace(txtMarca.Text) ? null : txtMarca.Text.Trim();
            string? modelo = string.IsNullOrWhiteSpace(txtModelo.Text) ? null : txtModelo.Text.Trim();
            grid.Rows.Clear();
            foreach (var p in Repository.GetProveedores(tipo, marca, modelo))
                grid.Rows.Add($"#{p.Id}", p.RazonSocial, p.Tipo.ToUpper(), p.Marcas, p.Modelos, p.Telefono);
        }
    }
}
