using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Sistema
{
    public class SucursalesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public SucursalesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            var btnNueva = AppTheme.MakeButton("➕  Nueva Sucursal", AppTheme.Accent, 160, 32);
            var btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnElim = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnNueva.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnNueva.Click += (s, e) => { new SucursalEditForm(null).ShowDialog(); LoadGrid(); };
            btnEditar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Sucursal su)
                { new SucursalEditForm(su).ShowDialog(); LoadGrid(); }
                else MessageBox.Show("Seleccioná una sucursal.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnElim.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Sucursal su)
                {
                    if (MessageBox.Show($"¿Eliminar la sucursal \"{su.Nombre}\"?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        bool baja = Repository.DeleteSucursal(su.Id);
                        if (baja) MessageBox.Show($"La sucursal \"{su.Nombre}\" ya tiene ventas, usuarios u otros movimientos, por eso no se borró: quedó Inactiva y se conserva el historial.\nPodés reactivarla desde Modificar.", "Sucursal dada de baja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadGrid();
                    }
                }
                else MessageBox.Show("Seleccioná una sucursal.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnNueva); flow.Controls.Add(btnEditar); flow.Controls.Add(btnElim);
            toolbar.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Nombre", "Nombre");
            grid.Columns.Add("Ciudad", "Ciudad");
            grid.Columns.Add("Direccion", "Dirección");
            grid.Columns.Add("Telefono", "Teléfono");
            grid.Columns.Add("Email", "Email");
            grid.Columns.Add("Estado", "Estado");
            grid.Columns["Id"].FillWeight = 35;
            grid.Columns["Estado"].FillWeight = 60;

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is Sucursal suc)
                {
                    new SucursalEditForm(suc).ShowDialog();
                    LoadGrid();
                }
            };

            Controls.Add(grid);
            Controls.Add(toolbar);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var s in Repository.GetSucursales())
            {
                int i = grid.Rows.Add($"#{s.Id}", s.Nombre, s.Ciudad, s.Direccion, s.Telefono, s.Email, s.Activa ? "Activa ✓" : "Inactiva");
                grid.Rows[i].Tag = s;
                if (!s.Activa) grid.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
            }
        }
    }

    public class SucursalEditForm : Form
    {
        private readonly Sucursal? _s;
        public SucursalEditForm(Sucursal? s) { _s = s; Build(); }

        private void Build()
        {
            Text = _s == null ? "Nueva Sucursal" : $"Editar: {_s.Nombre}";
            Size = new Size(440, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppTheme.White;

            int y = 16;
            var txtNombre   = AppTheme.MakeTextBox(390); txtNombre.Text   = _s?.Nombre    ?? "";
            var txtDireccion= AppTheme.MakeTextBox(390); txtDireccion.Text= _s?.Direccion ?? "";
            var txtCiudad   = AppTheme.MakeTextBox(180); txtCiudad.Text   = _s?.Ciudad    ?? "";
            var txtTelefono = AppTheme.MakeTextBox(180); txtTelefono.Text = _s?.Telefono  ?? "";
            var txtEmail    = AppTheme.MakeTextBox(390); txtEmail.Text    = _s?.Email     ?? "";

            void Field(string lbl, Control c)
            {
                Controls.Add(new Label { Text = lbl, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                y += 20; c.Left = 16; c.Top = y; Controls.Add(c); y += 34;
            }

            Field("Nombre *", txtNombre);
            Field("Dirección", txtDireccion);

            // Ciudad y Teléfono en la misma fila
            Controls.Add(new Label { Text = "Ciudad", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Teléfono", Left = 216, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20;
            txtCiudad.Left = 16; txtCiudad.Top = y; Controls.Add(txtCiudad);
            txtTelefono.Left = 216; txtTelefono.Top = y; Controls.Add(txtTelefono);
            y += 34;

            Field("Email", txtEmail);

            var chkActiva = new CheckBox { Text = "Sucursal activa", Checked = _s?.Activa ?? true, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontNormal };
            Controls.Add(chkActiva); y += 30;

            var btn = AppTheme.MakeButton("✓  Guardar", AppTheme.Accent, 130, 36);
            btn.Left = 16; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Repository.SaveSucursal(new Sucursal
                {
                    Id        = _s?.Id ?? 0,
                    Nombre    = txtNombre.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    Ciudad    = txtCiudad.Text.Trim(),
                    Telefono  = txtTelefono.Text.Trim(),
                    Email     = txtEmail.Text.Trim(),
                    Activa    = chkActiva.Checked
                });
                Close();
            };
            Controls.Add(btn);
        }
    }
}
