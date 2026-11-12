using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Sistema
{
    public class UsuariosForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public UsuariosForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            var btnNuevo = AppTheme.MakeButton("➕  Nuevo Usuario", AppTheme.Accent, 160, 32);
            var btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnElim = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnNuevo.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnNuevo.Click += (s, e) => { new UsuarioEditForm(null).ShowDialog(); LoadGrid(); };
            btnEditar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Usuario u)
                { new UsuarioEditForm(u).ShowDialog(); LoadGrid(); }
                else MessageBox.Show("Seleccioná un usuario.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnElim.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Usuario u)
                {
                    if (u.Id == _user.Id) { MessageBox.Show("No podés eliminar tu propio usuario mientras estás conectado.", "No permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (u.Rol == "sistema" && u.Activo && Repository.GetUsuarios().FindAll(x => x.Rol == "sistema" && x.Activo).Count <= 1)
                    { MessageBox.Show("No se puede eliminar el único usuario administrador (rol sistema).", "No permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (MessageBox.Show($"¿Eliminar el usuario \"{u.Username}\" ({u.NombreCompleto})?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    { Repository.DeleteUsuario(u.Id); LoadGrid(); }
                }
                else MessageBox.Show("Seleccioná un usuario.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnNuevo); flow.Controls.Add(btnEditar); flow.Controls.Add(btnElim);
            toolbar.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Nombre", "Nombre Completo");
            grid.Columns.Add("User", "Usuario"); grid.Columns.Add("Rol", "Rol");
            grid.Columns.Add("Suc", "Sucursal"); grid.Columns.Add("Estado", "Estado");
            grid.Columns["Id"].FillWeight = 35; grid.Columns["User"].FillWeight = 80; grid.Columns["Rol"].FillWeight = 70;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is Usuario u) { new UsuarioEditForm(u).ShowDialog(); LoadGrid(); } };

            Controls.Add(grid); Controls.Add(toolbar);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var u in Repository.GetUsuarios())
            {
                int i = grid.Rows.Add($"#{u.Id}", u.NombreCompleto, u.Username, u.Rol.ToUpper(), u.SucursalNombre, u.Activo ? "Activo" : "Inactivo");
                grid.Rows[i].Tag = u;
                if (!u.Activo) grid.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
            }
        }
    }

    public class UsuarioEditForm : Form
    {
        private readonly Usuario? _u;
        public UsuarioEditForm(Usuario? u) { _u = u; Build(); }

        private void Build()
        {
            Text = _u == null ? "Nuevo Usuario" : $"Editar: {_u.Username}";
            Size = new Size(460, 420); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; BackColor = AppTheme.White;

            int y = 16;
            var txtNombre   = AppTheme.MakeTextBox(200); txtNombre.Text = _u?.Nombre ?? "";
            var txtApellido = AppTheme.MakeTextBox(200); txtApellido.Text = _u?.Apellido ?? "";
            var txtEmail    = AppTheme.MakeTextBox(400); txtEmail.Text = _u?.Email ?? "";
            var txtUsername = AppTheme.MakeTextBox(180); txtUsername.Text = _u?.Username ?? "";
            if (_u != null) { txtUsername.ReadOnly = true; txtUsername.BackColor = Color.FromArgb(240, 240, 244); }   // el nombre de usuario no se cambia al editar
            var txtPassword = AppTheme.MakeTextBox(180); txtPassword.PasswordChar = '●';
            var cmbRol      = AppTheme.MakeCombo(180); cmbRol.Items.AddRange(new object[] { "ventas", "compras", "finanzas", "sistema" }); cmbRol.SelectedItem = _u?.Rol ?? "ventas";
            var cmbSuc      = AppTheme.MakeCombo(200); cmbSuc.Items.Add(new Sucursal { Id = 0, Nombre = "Sin sucursal" });
            foreach (var s in Repository.GetSucursales(true)) { cmbSuc.Items.Add(s); if (s.Id == _u?.SucursalId) cmbSuc.SelectedItem = s; }
            if (cmbSuc.SelectedIndex < 0) cmbSuc.SelectedIndex = 0;
            var chkActivo = new CheckBox { Text = "Usuario activo", Checked = _u?.Activo ?? true, Font = AppTheme.FontNormal };

            void Row2(string l1, string l2, Control c1, Control c2)
            {
                Controls.Add(new Label { Text = l1, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                Controls.Add(new Label { Text = l2, Left = 236, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                c1.Left = 16; c1.Top = y + 20; Controls.Add(c1);
                c2.Left = 236; c2.Top = y + 20; Controls.Add(c2);
                y += 56;
            }
            void Field(string l, Control c) { Controls.Add(new Label { Text = l, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 16; c.Top = y; c.Width = 420; Controls.Add(c); y += 36; }

            Row2("Nombre *", "Apellido *", txtNombre, txtApellido);
            Field("Email *", txtEmail);
            Row2("Usuario *", _u == null ? "Contraseña *" : "Nueva contraseña", txtUsername, txtPassword);
            Row2("Rol *", "Sucursal", cmbRol, cmbSuc);
            chkActivo.Left = 16; chkActivo.Top = y; Controls.Add(chkActivo); y += 32;

            var btn = AppTheme.MakeButton("✓  Guardar", AppTheme.Accent, 130, 36);
            btn.Left = 16; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtApellido.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
                { MessageBox.Show("Nombre, apellido, email y usuario son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (!txtEmail.Text.Contains('@')) { MessageBox.Show("Ingresá un email válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (_u == null && string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("La contraseña es obligatoria para nuevo usuario.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                // Siempre tiene que quedar al menos un administrador activo: si no, nadie puede gestionar usuarios.
                bool quedaAdmin = cmbRol.SelectedItem?.ToString() == "sistema" && chkActivo.Checked;
                if (_u is { Rol: "sistema", Activo: true } && !quedaAdmin &&
                    Repository.GetUsuarios().FindAll(x => x.Rol == "sistema" && x.Activo && x.Id != _u.Id).Count == 0)
                { MessageBox.Show("Es el único administrador activo (rol sistema): no se le puede cambiar el rol ni desactivarlo.", "No permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var suc = (Sucursal?)cmbSuc.SelectedItem;
                try
                {
                    Repository.SaveUsuario(new Usuario { Id = _u?.Id ?? 0, Nombre = txtNombre.Text.Trim(), Apellido = txtApellido.Text.Trim(), Email = txtEmail.Text.Trim(), Username = txtUsername.Text.Trim(), Rol = cmbRol.SelectedItem?.ToString() ?? "ventas", SucursalId = suc?.Id == 0 ? null : suc?.Id, Activo = chkActivo.Checked }, string.IsNullOrWhiteSpace(txtPassword.Text) ? null : txtPassword.Text);
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 19)
                { MessageBox.Show("Ya existe un usuario con ese nombre de usuario o ese email.", "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Close();
            };
            Controls.Add(btn);
        }
    }
}
