using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Sistema
{
    public class ClientesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;

        public ClientesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            // Toolbar
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            var btnNuevo = AppTheme.MakeButton("➕  Nuevo Cliente", AppTheme.Accent, 160, 32);
            var btnEditar= AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnCC    = AppTheme.MakeButton("💳  Cuenta Cte.", AppTheme.Info, 140, 32);
            var btnElim  = AppTheme.MakeButton("🗑  Eliminar", Color.FromArgb(220, 38, 38), 120, 32);
            btnNuevo.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnCC.Margin = new Padding(0, 0, 8, 0);
            btnNuevo.Click += (s, e) => { new ClienteEditForm(null).ShowDialog(); LoadGrid(); };
            btnEditar.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Cliente c)
                { new ClienteEditForm(c).ShowDialog(); LoadGrid(); }
                else MessageBox.Show("Seleccioná un cliente.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnCC.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Cliente c)
                    new CuentaCorrienteForm(c).ShowDialog();
                else
                    MessageBox.Show("Seleccioná un cliente.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnElim.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Cliente c)
                {
                    if (MessageBox.Show($"¿Eliminar el cliente \"{c.NombreCompleto}\"?\nSe borrará también su cuenta corriente y movimientos.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try { Repository.DeleteCliente(c.Id); LoadGrid(); }
                        catch (InvalidOperationException ex)
                        { MessageBox.Show(ex.Message, "No se puede eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    }
                }
                else MessageBox.Show("Seleccioná un cliente.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnNuevo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnCC);
            flow.Controls.Add(btnElim);
            toolbar.Controls.Add(flow);

            // Grid
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id",      "#");
            grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Nombre",  "Nombre");
            grid.Columns.Add("CUIT",    "CUIT/DNI");
            grid.Columns.Add("Tel",     "Teléfono");
            grid.Columns.Add("Email",   "Email");
            grid.Columns.Add("Saldo",   "Saldo CC");
            grid.Columns.Add("Limite",  "Límite");
            grid.Columns["Id"].FillWeight = 35;
            grid.Columns["Saldo"].DefaultCellStyle.Format  = "C2";
            grid.Columns["Limite"].DefaultCellStyle.Format = "C2";

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is Cliente c)
                {
                    new ClienteEditForm(c).ShowDialog();
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
            foreach (var c in Repository.GetClientes())
            {
                int i = grid.Rows.Add($"#{c.Id}", c.NombreCompleto, c.DniCuit, c.Telefono, c.Email, c.Saldo, c.LimiteCredito);
                grid.Rows[i].Tag = c;
                if (c.Saldo < 0) grid.Rows[i].Cells["Saldo"].Style.ForeColor = Color.Crimson;
            }
        }
    }

    // ── Editar / nuevo cliente ────────────────────────────────────────────────
    public class ClienteEditForm : Form
    {
        private readonly Cliente? _c;
        public ClienteEditForm(Cliente? c) { _c = c; Build(); }

        private void Build()
        {
            Text = _c == null ? "Nuevo Cliente Mayorista" : $"Editar: {_c.NombreCompleto}";
            Size = new Size(460, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppTheme.White;

            int y = 16;
            var txtNombre   = AppTheme.MakeTextBox(200); txtNombre.Text   = _c?.Nombre    ?? "";
            var txtApellido = AppTheme.MakeTextBox(200); txtApellido.Text = _c?.Apellido  ?? "";
            var txtDni      = AppTheme.MakeTextBox(390); txtDni.Text      = _c?.DniCuit   ?? "";
            var txtTel      = AppTheme.MakeTextBox(180); txtTel.Text      = _c?.Telefono  ?? "";
            var txtEmail    = AppTheme.MakeTextBox(180); txtEmail.Text    = _c?.Email     ?? "";
            var txtDir      = AppTheme.MakeTextBox(390); txtDir.Text      = _c?.Direccion ?? "";
            var nudLimite   = new NumericUpDown { Width = 180, DecimalPlaces = 2, Maximum = 9999999, Value = (decimal)(_c?.LimiteCredito ?? 0), Font = AppTheme.FontNormal };

            // Nombre / Apellido en fila
            Controls.Add(new Label { Text = "Nombre *", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Apellido",  Left = 236, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20;
            txtNombre.Left = 16; txtNombre.Top = y; Controls.Add(txtNombre);
            txtApellido.Left = 236; txtApellido.Top = y; Controls.Add(txtApellido);
            y += 36;

            void Field(string lbl, Control c)
            {
                Controls.Add(new Label { Text = lbl, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                y += 20; c.Left = 16; c.Top = y; Controls.Add(c); y += 34;
            }

            Field("CUIT / DNI", txtDni);

            Controls.Add(new Label { Text = "Teléfono", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Email",    Left = 216, Top = y, AutoSize = true, Font = AppTheme.FontBold });
            y += 20;
            txtTel.Left = 16; txtTel.Top = y; Controls.Add(txtTel);
            txtEmail.Left = 216; txtEmail.Top = y; Controls.Add(txtEmail);
            y += 36;

            Field("Dirección", txtDir);

            if (_c == null)
            {
                Controls.Add(new Label { Text = "Límite de crédito $", Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                y += 20; nudLimite.Left = 16; nudLimite.Top = y; Controls.Add(nudLimite); y += 34;
            }

            var btn = AppTheme.MakeButton("✓  Guardar", AppTheme.Accent, 130, 36);
            btn.Left = 16; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Repository.SaveCliente(new Cliente
                {
                    Id           = _c?.Id ?? 0,
                    Nombre       = txtNombre.Text.Trim(),
                    Apellido     = txtApellido.Text.Trim(),
                    DniCuit      = txtDni.Text.Trim(),
                    Telefono     = txtTel.Text.Trim(),
                    Email        = txtEmail.Text.Trim(),
                    Direccion    = txtDir.Text.Trim(),
                    LimiteCredito= nudLimite.Value
                });
                Close();
            };
            Controls.Add(btn);
        }
    }

    // ── Cuenta corriente ──────────────────────────────────────────────────────
    public class CuentaCorrienteForm : Form
    {
        private readonly Cliente _cliente;
        private DataGridView grid = null!;
        private Label lblSaldo = null!;

        public CuentaCorrienteForm(Cliente c) { _cliente = c; Build(); }

        private void Build()
        {
            Text = $"Cuenta Corriente — {_cliente.NombreCompleto}";
            Size = new Size(760, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.LightBg;

            // Panel info + nueva transacción (izquierda)
            var left = new Panel { Width = 260, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);

            int y = 10;
            left.Controls.Add(new Label { Text = "👤  " + _cliente.NombreCompleto, Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, Width = 230, AutoSize = false }); y += 22;
            left.Controls.Add(new Label { Text = $"CUIT/DNI: {_cliente.DniCuit}", Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted, Left = 0, Top = y, AutoSize = true }); y += 18;
            left.Controls.Add(new Label { Text = $"Límite: {_cliente.LimiteCredito:C2}", Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted, Left = 0, Top = y, AutoSize = true }); y += 26;

            lblSaldo = new Label { Left = 0, Top = y, Width = 230, AutoSize = false, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
            left.Controls.Add(lblSaldo); y += 36;

            var sep = new Label { Left = 0, Top = y, Width = 230, Height = 1, BackColor = Color.FromArgb(220, 220, 230) };
            left.Controls.Add(sep); y += 14;

            left.Controls.Add(new Label { Text = "📝  Nueva Transacción", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 26;

            var cmbTipo = AppTheme.MakeCombo(230);
            cmbTipo.Items.Add("credito"); cmbTipo.Items.Add("debito"); cmbTipo.SelectedIndex = 0;

            var nudMonto = new NumericUpDown { Width = 230, DecimalPlaces = 2, Maximum = 9999999, Minimum = 0.01m, Value = 1, Font = AppTheme.FontNormal };
            var txtDesc  = AppTheme.MakeTextBox(230, true); txtDesc.Height = 55;

            void Field(string lbl, Control c)
            {
                left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                y += 20; c.Left = 0; c.Top = y; c.Width = 230; left.Controls.Add(c); y += (c.Height + 10);
            }

            Field("Tipo", cmbTipo);
            Field("Monto $", nudMonto);
            Field("Descripción", txtDesc);

            var btnOk = AppTheme.MakeButton("✓  Registrar", AppTheme.Accent, 130, 34);
            btnOk.Left = 0; btnOk.Top = y;
            btnOk.Click += (s, e) =>
            {
                Repository.RegistrarTransaccionCC(
                    _cliente.Id,
                    cmbTipo.SelectedItem?.ToString() ?? "credito",
                    nudMonto.Value,
                    txtDesc.Text.Trim());
                txtDesc.Clear(); nudMonto.Value = 1;
                RefreshSaldo(); LoadGrid();
            };
            left.Controls.Add(btnOk);

            // Grid movimientos (derecha)
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Fecha",  "Fecha");
            grid.Columns.Add("Tipo",   "Tipo");
            grid.Columns.Add("Monto",  "Monto");
            grid.Columns.Add("Desc",   "Descripción");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";
            right.Controls.Add(grid);

            Controls.Add(right);
            Controls.Add(left);

            RefreshSaldo();
            LoadGrid();
        }

        private void RefreshSaldo()
        {
            // Recargar saldo desde la BD
            var clientes = Repository.GetClientes();
            foreach (var c in clientes)
            {
                if (c.Id == _cliente.Id)
                {
                    lblSaldo.Text      = $"Saldo: {c.Saldo:C2}";
                    lblSaldo.ForeColor = c.Saldo < 0 ? Color.Crimson : AppTheme.Success;
                    break;
                }
            }
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT t.fecha, t.tipo, t.monto, COALESCE(t.descripcion,'')
                                FROM transacciones_cc t
                                JOIN cuentas_corrientes cc ON t.cuenta_id = cc.id
                                WHERE cc.cliente_id = @cid
                                ORDER BY t.id DESC";
            cmd.Parameters.AddWithValue("@cid", _cliente.Id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                decimal monto = (decimal)r.GetDouble(2);
                bool esCredito = r.GetString(1) == "credito";
                int i = grid.Rows.Add(r.GetString(0), esCredito ? "Crédito ▲" : "Débito ▼", monto, r.GetString(3));
                grid.Rows[i].DefaultCellStyle.ForeColor = esCredito ? Color.FromArgb(22, 163, 74) : Color.Crimson;
            }
        }
    }
}
