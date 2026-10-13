using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Ventas
{
    public class EnviosForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;

        public EnviosForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "📤  Registrar Envío a Central", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            var cmbSuc = AppTheme.MakeCombo(260);
            foreach (var s in Repository.GetSucursales(true)) { cmbSuc.Items.Add(s); if (s.Id == _user.SucursalId) cmbSuc.SelectedItem = s; }
            if (cmbSuc.SelectedIndex < 0 && cmbSuc.Items.Count > 0) cmbSuc.SelectedIndex = 0;
            var txtPeriodo = AppTheme.MakeTextBox(260); txtPeriodo.Text = $"1ra quincena {DateTime.Today:MMMM yyyy}";
            var nudItems = new NumericUpDown { Width = 260, Minimum = 0, Maximum = 9999, Value = 0, Font = AppTheme.FontNormal };
            var txtObs = AppTheme.MakeTextBox(260, true);

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 260; left.Controls.Add(c); y += 36; }
            Field("Sucursal *", cmbSuc); Field("Período *", txtPeriodo); Field("Cantidad de ítems *", nudItems); Field("Observaciones", txtObs);

            y += 44;
            var btn = AppTheme.MakeButton("📤  Registrar", AppTheme.Accent, 140, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPeriodo.Text)) { MessageBox.Show("Completá el período.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var suc = (Sucursal?)cmbSuc.SelectedItem;
                Repository.SaveEnvio(new EnvioListado { SucursalId = suc?.Id ?? 0, FechaEnvio = DateTime.Today.ToString("yyyy-MM-dd"), Periodo = txtPeriodo.Text.Trim(), CantidadItems = (int)nudItems.Value, Observaciones = txtObs.Text.Trim() });
                MessageBox.Show("Envío registrado.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtObs.Clear(); LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            var rtool = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = AppTheme.LightBg, Padding = new Padding(8, 6, 8, 6) };
            var btnElim = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnElim.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is EnvioListado env)
                {
                    if (MessageBox.Show($"¿Eliminar el envío #{env.Id} ({env.Periodo})?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    { Repository.DeleteEnvio(env.Id); LoadGrid(); }
                }
                else MessageBox.Show("Seleccioná un envío.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            rtool.Controls.Add(btnElim);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha"); grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Periodo", "Período"); grid.Columns.Add("Items", "Ítems"); grid.Columns.Add("Obs", "Observaciones");
            right.Controls.Add(grid);
            right.Controls.Add(rtool);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var e in Repository.GetEnvios())
            {
                int i = grid.Rows.Add($"#{e.Id}", e.FechaEnvio, e.SucursalNombre, e.Periodo, e.CantidadItems, e.Observaciones);
                grid.Rows[i].Tag = e;
            }
        }
    }
}
