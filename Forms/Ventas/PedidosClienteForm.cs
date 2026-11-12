using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Ventas
{
    public class PedidosClienteForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        private ComboBox cmbSuc = null!;
        private TextBox txtCliente = null!, txtMarca = null!, txtModelo = null!, txtObs = null!;
        private NumericUpDown nudCant = null!;
        private Button btn = null!;
        private int _editId = 0;

        public PedidosClienteForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "📝  Registrar Pedido", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            cmbSuc = AppTheme.MakeCombo(260);
            foreach (var s in Repository.GetSucursales(true)) { cmbSuc.Items.Add(s); if (s.Id == _user.SucursalId) cmbSuc.SelectedItem = s; }
            if (cmbSuc.SelectedIndex < 0 && cmbSuc.Items.Count > 0) cmbSuc.SelectedIndex = 0;

            txtCliente = AppTheme.MakeTextBox(260);
            txtMarca = AppTheme.MakeTextBox(260);
            txtModelo = AppTheme.MakeTextBox(260);
            nudCant = new NumericUpDown { Width = 260, Minimum = 1, Maximum = 999, Value = 1, Font = AppTheme.FontNormal };
            txtObs = AppTheme.MakeTextBox(260, true);

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 260; left.Controls.Add(c); y += 36; }
            Field("Sucursal *", cmbSuc); Field("Nombre del cliente *", txtCliente);
            Field("Marca *", txtMarca); Field("Modelo *", txtModelo); Field("Cantidad *", nudCant); Field("Observaciones", txtObs);

            y += 44;
            btn = AppTheme.MakeButton("➕  Registrar", AppTheme.Accent, 160, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCliente.Text) || string.IsNullOrWhiteSpace(txtMarca.Text) || string.IsNullOrWhiteSpace(txtModelo.Text)) { MessageBox.Show("Completá los campos obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var suc = (Sucursal?)cmbSuc.SelectedItem;
                var p = new PedidoCliente { Id = _editId, SucursalId = suc?.Id ?? 0, ClienteNombre = txtCliente.Text.Trim(), Marca = txtMarca.Text.Trim(), Modelo = txtModelo.Text.Trim(), Cantidad = (int)nudCant.Value, Fecha = DateTime.Today.ToString("yyyy-MM-dd"), Observaciones = txtObs.Text.Trim() };
                if (_editId == 0) Repository.SavePedidoCliente(p);
                else Repository.UpdatePedidoCliente(p);
                ResetForm();
                LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            var rtool = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = AppTheme.LightBg, Padding = new Padding(8, 6, 8, 6) };
            var btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnElim = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is PedidoCliente p) CargarParaEditar(p);
                else MessageBox.Show("Seleccioná un pedido.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnElim.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is PedidoCliente p)
                {
                    if (MessageBox.Show($"¿Eliminar el pedido de \"{p.ClienteNombre}\" ({p.Marca} {p.Modelo})?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    { Repository.DeletePedidoCliente(p.Id); ResetForm(); LoadGrid(); }
                }
                else MessageBox.Show("Seleccioná un pedido.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnEditar); flow.Controls.Add(btnElim);
            rtool.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha"); grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Cliente", "Cliente"); grid.Columns.Add("Marca", "Marca");
            grid.Columns.Add("Modelo", "Modelo"); grid.Columns.Add("Cant", "Cant.");
            right.Controls.Add(grid);
            right.Controls.Add(rtool);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void CargarParaEditar(PedidoCliente p)
        {
            _editId = p.Id;
            foreach (Sucursal it in cmbSuc.Items) if (it.Id == p.SucursalId) { cmbSuc.SelectedItem = it; break; }
            txtCliente.Text = p.ClienteNombre;
            txtMarca.Text = p.Marca;
            txtModelo.Text = p.Modelo;
            nudCant.Value = Math.Clamp(p.Cantidad, nudCant.Minimum, nudCant.Maximum);
            txtObs.Text = p.Observaciones;
            btn.Text = "✓  Guardar cambios";
        }

        private void ResetForm()
        {
            _editId = 0;
            txtCliente.Clear(); txtMarca.Clear(); txtModelo.Clear(); txtObs.Clear(); nudCant.Value = 1;
            btn.Text = "➕  Registrar";
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var p in Repository.GetPedidosCliente())
            {
                int i = grid.Rows.Add($"#{p.Id}", p.Fecha, p.SucursalNombre, p.ClienteNombre, p.Marca, p.Modelo, p.Cantidad);
                grid.Rows[i].Tag = p;
            }
        }
    }
}
