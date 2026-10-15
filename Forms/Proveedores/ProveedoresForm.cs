using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Proveedores
{
    public class ProveedoresForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public ProveedoresForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            var btnNuevo = AppTheme.MakeButton("➕  Nuevo Proveedor", AppTheme.Accent, 170, 32);
            var btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnEliminar = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnNuevo.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnNuevo.Click += (s, e) => { new ProveedorEditForm(null, _user).ShowDialog(); LoadGrid(); };
            btnEditar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Proveedor p)
                { new ProveedorEditForm(p, _user).ShowDialog(); LoadGrid(); }
                else MessageBox.Show("Seleccioná un proveedor.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnEliminar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Proveedor p)
                {
                    if (MessageBox.Show($"¿Eliminar el proveedor \"{p.RazonSocial}\"?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        bool baja = Repository.DeleteProveedor(p.Id);
                        if (baja) MessageBox.Show($"El proveedor \"{p.RazonSocial}\" ya tiene cotizaciones registradas, por eso no se borró: quedó dado de baja y se conserva el historial.", "Proveedor dado de baja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadGrid();
                    }
                }
                else MessageBox.Show("Seleccioná un proveedor.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnNuevo); flow.Controls.Add(btnEditar); flow.Controls.Add(btnEliminar);
            toolbar.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("RS", "Razón Social");
            grid.Columns.Add("Tipo", "Tipo"); grid.Columns.Add("Marcas", "Marcas");
            grid.Columns.Add("Tel", "Teléfono"); grid.Columns.Add("Email", "Email");
            grid.Columns["Id"].FillWeight = 35; grid.Columns["Tipo"].FillWeight = 60;

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Tag is not Proveedor p) return;
                new ProveedorEditForm(p, _user).ShowDialog(); LoadGrid();
            };

            Controls.Add(grid);
            Controls.Add(toolbar);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var p in Repository.GetProveedores())
            {
                int i = grid.Rows.Add($"#{p.Id}", p.RazonSocial, p.Tipo.ToUpper(), p.Marcas, p.Telefono, p.Email);
                grid.Rows[i].Tag = p;
                if (p.Tipo == "fijo") grid.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
            }
        }
    }

    public class ProveedorEditForm : Form
    {
        private readonly Proveedor? _p;
        private readonly Usuario _user;
        public ProveedorEditForm(Proveedor? p, Usuario user) { _p = p; _user = user; Build(); }

        private void Build()
        {
            Text = _p == null ? "Nuevo Proveedor" : $"Editar: {_p.RazonSocial}";
            Size = new Size(480, 540); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = AppTheme.White; Padding = new Padding(20);

            int y = 16;
            var txtRS     = AppTheme.MakeTextBox(400); txtRS.Text = _p?.RazonSocial ?? "";
            var txtCuit   = AppTheme.MakeTextBox(200); txtCuit.Text = _p?.Cuit ?? "";
            var cmbTipo   = AppTheme.MakeCombo(200); cmbTipo.Items.AddRange(new object[] { "fijo", "alternativo" }); cmbTipo.SelectedItem = _p?.Tipo ?? "fijo";
            var txtTel    = AppTheme.MakeTextBox(200); txtTel.Text = _p?.Telefono ?? "";
            var txtEmail  = AppTheme.MakeTextBox(200); txtEmail.Text = _p?.Email ?? "";
            var txtDir    = AppTheme.MakeTextBox(400); txtDir.Text = _p?.Direccion ?? "";
            var txtMarcas = AppTheme.MakeTextBox(400); txtMarcas.Text = _p?.Marcas ?? "";
            var txtModelos= AppTheme.MakeTextBox(400); txtModelos.Text = _p?.Modelos ?? "";

            void Field(string lbl, Control c, int w = 400) { Controls.Add(new Label { Text = lbl, Left = 20, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 20; c.Top = y; c.Width = w; Controls.Add(c); y += 32; }
            Field("Razón Social *", txtRS);
            Field("CUIT", txtCuit, 200); txtCuit.Left = 20;
            Field("Tipo", cmbTipo, 200);
            Field("Teléfono", txtTel, 200); Field("Email", txtEmail, 200);
            Field("Dirección", txtDir);
            Field("Marcas que abastece", txtMarcas);
            Field("Modelos que abastece", txtModelos);

            var btn = AppTheme.MakeButton("✓  Guardar", AppTheme.Accent, 130, 36);
            btn.Left = 20; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtRS.Text)) { MessageBox.Show("Razón social es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Repository.SaveProveedor(new Proveedor { Id = _p?.Id ?? 0, RazonSocial = txtRS.Text.Trim(), Cuit = txtCuit.Text.Trim(), Tipo = cmbTipo.SelectedItem?.ToString() ?? "fijo", Telefono = txtTel.Text.Trim(), Email = txtEmail.Text.Trim(), Direccion = txtDir.Text.Trim(), Marcas = txtMarcas.Text.Trim(), Modelos = txtModelos.Text.Trim() });
                Close();
            };
            Controls.Add(btn);
            ClientSize = new Size(ClientSize.Width, btn.Bottom + 24);   // que el botón Guardar quede siempre visible
        }
    }
}
