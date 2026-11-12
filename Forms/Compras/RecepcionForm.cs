using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;
using Microsoft.Data.Sqlite;

namespace KawaSysWF.Forms.Compras
{
    public class RecepcionForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public RecepcionForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var left = new Panel { Width = 320, Dock = DockStyle.Left, BackColor = AppTheme.White, Padding = new Padding(16) };
            left.AutoScroll = true;
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), left.Width - 1, 0, left.Width - 1, left.Height);
            int y = 10;
            left.Controls.Add(new Label { Text = "🚚  Registrar Pedido Pendiente", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary, Left = 0, Top = y, AutoSize = true }); y += 30;

            var cmbOrden = AppTheme.MakeCombo(290);
            foreach (var o in Repository.GetOrdenesCompra()) if (o.Estado == "aprobada") cmbOrden.Items.Add(o);
            if (cmbOrden.Items.Count > 0) cmbOrden.SelectedIndex = 0;

            var dtDespacho = new DateTimePicker { Width = 290, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var dtLlegada  = new DateTimePicker { Width = 290, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(15) };
            var txtDesc = new TextBox { Width = 290, Height = 60, Multiline = true, Font = AppTheme.FontNormal, BorderStyle = BorderStyle.FixedSingle };

            void Field(string lbl, Control c) { left.Controls.Add(new Label { Text = lbl, Left = 0, Top = y, AutoSize = true, Font = AppTheme.FontBold }); y += 20; c.Left = 0; c.Top = y; c.Width = 290; left.Controls.Add(c); y += 36; }
            Field("Orden aprobada *", cmbOrden);
            Field("Fecha de despacho *", dtDespacho);
            Field("Fecha estimada de llegada *", dtLlegada);
            Field("Descripción de la mercadería", txtDesc);

            y += 44;
            var btn = AppTheme.MakeButton("🚚  Registrar", AppTheme.Accent, 130, 36);
            btn.Left = 0; btn.Top = y;
            btn.Click += (s, e) =>
            {
                if (cmbOrden.SelectedItem is not OrdenCompra o) { MessageBox.Show("Seleccioná una orden aprobada.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (dtLlegada.Value.Date < dtDespacho.Value.Date)
                { MessageBox.Show("La fecha estimada de llegada no puede ser anterior a la fecha de despacho.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                using var conn = DBHelper.GetConnection();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO pedidos_recepcion(orden_id,fecha_despacho,fecha_estimada_llegada,descripcion) VALUES(@oid,@fd,@fl,@d)";
                cmd.Parameters.AddWithValue("@oid", o.Id);
                cmd.Parameters.AddWithValue("@fd", dtDespacho.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@fl", dtLlegada.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@d", txtDesc.Text.Trim());
                cmd.ExecuteNonQuery();
                MessageBox.Show("✅  Pedido pendiente registrado.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtDesc.Clear(); LoadGrid();
            };
            left.Controls.Add(btn);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Orden", "Orden"); grid.Columns.Add("Despacho", "Despacho");
            grid.Columns.Add("Llegada", "Llegada est."); grid.Columns.Add("Estado", "Estado");
            right.Controls.Add(grid);
            Controls.Add(right); Controls.Add(left);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,orden_id,COALESCE(fecha_despacho,''),COALESCE(fecha_estimada_llegada,''),COALESCE(estado,'') FROM pedidos_recepcion ORDER BY id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                grid.Rows.Add($"#{r.GetInt32(0)}", $"OC-{r.GetInt32(1):D3}", r.GetString(2), r.GetString(3), r.GetString(4).Replace("_", " ").ToUpper());
        }
    }
}
