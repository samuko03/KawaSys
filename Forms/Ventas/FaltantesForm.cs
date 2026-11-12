using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Ventas
{
    public class FaltantesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        private ComboBox cmbSucursal = null!;
        private TextBox txtMarca = null!, txtModelo = null!, txtPeriodo = null!, txtObs = null!;
        private NumericUpDown nudCant = null!;
        private int _editId = 0;
        private Button btnAdd = null!;

        public FaltantesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            // Left panel – form
            var left = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "⚠  Registrar Faltante", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 260; left.Controls.Add(c); y += 36; }

            cmbSucursal = AppTheme.MakeCombo(260);
            foreach (var s in Repository.GetSucursales(true)) { cmbSucursal.Items.Add(s); if (s.Id == _user.SucursalId) cmbSucursal.SelectedItem = s; }
            if (cmbSucursal.SelectedIndex < 0 && cmbSucursal.Items.Count > 0) cmbSucursal.SelectedIndex = 0;
            Field("Sucursal *", cmbSucursal);

            txtMarca = AppTheme.MakeTextBox(260); Field("Marca *", txtMarca);
            txtModelo = AppTheme.MakeTextBox(260); Field("Modelo *", txtModelo);
            nudCant = new NumericUpDown { Width = 260, Minimum = 1, Maximum = 9999, Value = 1, Font = AppTheme.FontNormal };
            Field("Cantidad solicitada *", nudCant);
            txtPeriodo = AppTheme.MakeTextBox(260); txtPeriodo.Text = $"1ra quincena {DateTime.Today:MMMM yyyy}"; Field("Período *", txtPeriodo);
            txtObs = AppTheme.MakeTextBox(260, true); Field("Observaciones", txtObs);
            y += 44;

            btnAdd = AppTheme.MakeButton("➕  Agregar", AppTheme.Accent, 130, 36);
            btnAdd.Left = 0; btnAdd.Top = y; btnAdd.Click += DoSave;
            left.Controls.Add(btnAdd);

            // Right panel – grid
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg, Padding = new Padding(12) };
            var toolbar = new FlowLayoutPanel { Height = 40, Dock = DockStyle.Top, BackColor = AppTheme.LightBg };
            var btnModif = AppTheme.MakeButton("✏ Modificar", AppTheme.Info, 110, 32);
            var btnEmitir = AppTheme.MakeButton("📤 Emitir", Color.FromArgb(34, 197, 94), 110, 32);
            var btnElim = AppTheme.MakeButton("🗑 Eliminar", Color.FromArgb(220, 38, 38), 110, 32);
            btnModif.Margin = new Padding(0, 0, 6, 0);
            btnEmitir.Margin = new Padding(0, 0, 6, 0);
            btnModif.Click += (s, e) =>
            {
                if (GetSel() is ModeloFaltante f)
                {
                    if (f.Emitido) { MessageBox.Show("No se puede modificar un faltante ya emitido.", "No permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    CargarParaEditar(f);
                }
                else MessageBox.Show("Seleccioná un faltante.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnEmitir.Click += (s, e) =>
            {
                if (GetSel() is not ModeloFaltante f) { MessageBox.Show("Seleccioná un faltante.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                if (f.Emitido) { MessageBox.Show("Ese faltante ya fue emitido.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                Repository.EmitirFaltante(f.Id); LoadGrid();
            };
            btnElim.Click += (s, e) =>
            {
                if (GetSel() is not ModeloFaltante f) { MessageBox.Show("Seleccioná un faltante.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                if (f.Emitido) { MessageBox.Show("No se puede eliminar un faltante ya emitido.", "No permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (MessageBox.Show($"¿Eliminar el faltante {f.Marca} {f.Modelo}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                Repository.DeleteFaltante(f.Id); ResetForm(); LoadGrid();
            };
            toolbar.Controls.Add(btnModif); toolbar.Controls.Add(btnEmitir); toolbar.Controls.Add(btnElim);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Marca", "Marca"); grid.Columns.Add("Modelo", "Modelo");
            grid.Columns.Add("Cant", "Cant."); grid.Columns.Add("Periodo", "Período");
            grid.Columns.Add("Estado", "Estado");
            grid.Columns["Id"].FillWeight = 35;

            right.Controls.Add(grid);
            right.Controls.Add(toolbar);
            Controls.Add(right);
            Controls.Add(left);
            LoadGrid();
        }

        private ModeloFaltante? GetSel()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as ModeloFaltante;
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var f in Repository.GetFaltantes(_user.Rol == "ventas" ? _user.SucursalId : null))
            {
                int i = grid.Rows.Add($"#{f.Id}", f.SucursalNombre, f.Marca, f.Modelo, f.CantidadSolicitada, f.Periodo, f.Emitido ? "Emitido ✓" : "Pendiente");
                grid.Rows[i].Tag = f;
                if (f.Emitido) grid.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
            }
        }

        private void CargarParaEditar(ModeloFaltante f)
        {
            _editId = f.Id;
            foreach (Sucursal it in cmbSucursal.Items) if (it.Id == f.SucursalId) { cmbSucursal.SelectedItem = it; break; }
            txtMarca.Text = f.Marca;
            txtModelo.Text = f.Modelo;
            nudCant.Value = Math.Clamp(f.CantidadSolicitada, nudCant.Minimum, nudCant.Maximum);
            txtPeriodo.Text = f.Periodo;
            txtObs.Text = f.Observaciones;
            btnAdd.Text = "✓  Guardar cambios";
        }

        private void ResetForm()
        {
            _editId = 0;
            txtMarca.Clear(); txtModelo.Clear(); txtObs.Clear(); nudCant.Value = 1;
            btnAdd.Text = "➕  Agregar";
        }

        private void DoSave(object? s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModelo.Text) || string.IsNullOrWhiteSpace(txtMarca.Text))
            { MessageBox.Show("Marca y modelo son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPeriodo.Text))
            { MessageBox.Show("El período es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbSucursal.SelectedItem == null)
            { MessageBox.Show("Elegí una sucursal.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var suc = (Sucursal?)cmbSucursal.SelectedItem;
            Repository.SaveFaltante(new ModeloFaltante
            {
                Id = _editId,
                SucursalId = suc?.Id ?? 0, Marca = txtMarca.Text.Trim(), Modelo = txtModelo.Text.Trim(),
                CantidadSolicitada = (int)nudCant.Value, FechaGeneracion = DateTime.Today.ToString("yyyy-MM-dd"),
                Periodo = txtPeriodo.Text.Trim(), Observaciones = txtObs.Text.Trim()
            });
            ResetForm();
            LoadGrid();
        }
    }
}
