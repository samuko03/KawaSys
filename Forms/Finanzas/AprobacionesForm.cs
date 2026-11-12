using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Finanzas
{
    public class AprobacionesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public AprobacionesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);

            var btnAprobar = AppTheme.MakeButton("✅  Aprobar", Color.FromArgb(22, 163, 74), 120, 32);
            var btnRechazar = AppTheme.MakeButton("❌  Rechazar", Color.FromArgb(220, 38, 38), 120, 32);
            btnAprobar.Margin = new Padding(0, 0, 8, 0);
            btnAprobar.Click  += (s, e) => Resolver("aprobada");
            btnRechazar.Click += (s, e) => Resolver("rechazada");
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.Add(btnAprobar); flow.Controls.Add(btnRechazar);
            toolbar.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Orden", "Orden"); grid.Columns.Add("Desc", "Descripción"); grid.Columns.Add("Monto", "Monto");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";
            grid.Columns["Id"].FillWeight = 35;

            Controls.Add(grid);
            Controls.Add(toolbar);
            LoadGrid();
        }

        private Operacion? GetSel()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as Operacion;
        }

        private void Resolver(string resultado)
        {
            if (GetSel() is not Operacion op) { MessageBox.Show("Seleccioná una operación.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (resultado == "aprobada" && MessageBox.Show($"¿Aprobar la operación \"{op.Descripcion}\" por {op.Monto:C2}?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            string justif = "";
            if (resultado == "rechazada")
            {
                justif = Microsoft.VisualBasic.Interaction.InputBox("Ingresá la justificación del rechazo:", "Rechazar operación", "");
                if (string.IsNullOrWhiteSpace(justif)) return;
            }
            Repository.ResolverOperacion(op.Id, resultado, justif);
            MessageBox.Show($"✅  Operación {resultado}.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var op in Repository.GetOperaciones("pendiente"))
            {
                int i = grid.Rows.Add($"#{op.Id}", op.Fecha, op.OrdenId.HasValue ? $"OC-{op.OrdenId:D3}" : "-", op.Descripcion, op.Monto);
                grid.Rows[i].Tag = op;
            }
        }
    }

    public class HistorialOperacionesForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        public HistorialOperacionesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha"); grid.Columns.Add("Resolucion", "Resolución");
            grid.Columns.Add("Desc", "Descripción"); grid.Columns.Add("Monto", "Monto");
            grid.Columns.Add("Resultado", "Resultado"); grid.Columns.Add("Justif", "Justificación");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";

            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            var btnMod = AppTheme.MakeButton("✏ Modificar", AppTheme.Info, 120, 32);
            btnMod.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Operacion op)
                { MessageBox.Show("Seleccioná una operación.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                var form = new Form { Text = $"Modificar Op. #{op.Id}", Size = new Size(400, 200), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
                var cmbRes = AppTheme.MakeCombo(180); cmbRes.Items.AddRange(new object[] { "aprobada", "rechazada" }); cmbRes.SelectedItem = op.Resultado;
                var txtJ = AppTheme.MakeTextBox(280, true); txtJ.Text = op.Justificacion;
                form.Controls.Add(new Label { Text = "Resultado:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
                cmbRes.Left = 10; cmbRes.Top = 32; form.Controls.Add(cmbRes);
                form.Controls.Add(new Label { Text = "Justificación:", Left = 200, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
                txtJ.Left = 200; txtJ.Top = 32; form.Controls.Add(txtJ);
                var btn2 = AppTheme.MakeButton("Guardar", AppTheme.Accent, 100, 30); btn2.Left = 10; btn2.Top = 110;
                btn2.Click += (s2, e2) => {
                    if (cmbRes.SelectedItem?.ToString() == "rechazada" && string.IsNullOrWhiteSpace(txtJ.Text))
                    { MessageBox.Show("Para rechazar una operación ingresá la justificación.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    Repository.ResolverOperacion(op.Id, cmbRes.SelectedItem?.ToString() ?? "aprobada", txtJ.Text.Trim()); form.Close(); LoadGrid(); };
                form.Controls.Add(btn2); form.ShowDialog();
            };
            toolbar.Controls.Add(btnMod);

            Controls.Add(grid); Controls.Add(toolbar);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var op in Repository.GetOperaciones())
            {
                if (op.Resultado == "pendiente") continue;
                int i = grid.Rows.Add($"#{op.Id}", op.Fecha, op.FechaResolucion, op.Descripcion, op.Monto, op.Resultado.ToUpper(), op.Justificacion);
                grid.Rows[i].Tag = op;
                grid.Rows[i].DefaultCellStyle.BackColor = op.Resultado == "aprobada" ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
            }
        }
    }

    public class ReporteMensualForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        private Label lblStats = null!;
        public ReporteMensualForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var filter = new Panel { Height = 70, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            var cmbMes = AppTheme.MakeCombo(130); cmbMes.Items.Add("Todos");
            foreach (var m in new[] { "Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" }) cmbMes.Items.Add(m);
            cmbMes.SelectedIndex = 0;
            var cmbAnio = AppTheme.MakeCombo(90); cmbAnio.Items.Add("Todos");
            for (int y = 2023; y <= DateTime.Today.Year + 1; y++) cmbAnio.Items.Add(y);
            cmbAnio.SelectedIndex = 0;   // "Todos": los datos pueden ser de años anteriores

            filter.Controls.Add(new Label { Text = "Mes:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbMes.Left = 10; cmbMes.Top = 32; filter.Controls.Add(cmbMes);
            filter.Controls.Add(new Label { Text = "Año:", Left = 160, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbAnio.Left = 160; cmbAnio.Top = 32; filter.Controls.Add(cmbAnio);
            var btn = AppTheme.MakeButton("🔍 Ver", AppTheme.Accent, 90, 30); btn.Left = 260; btn.Top = 30; btn.Click += (s, e) => LoadGrid(cmbMes.SelectedIndex, cmbAnio.SelectedItem is int a ? a : (int?)null); filter.Controls.Add(btn);
            lblStats = new Label { Left = 370, Top = 22, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Primary }; filter.Controls.Add(lblStats);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Id", "#");
            grid.Columns["Id"].Visible = false; grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Desc", "Descripción"); grid.Columns.Add("Monto", "Monto");
            grid.Columns.Add("Resultado", "Resultado"); grid.Columns.Add("Justif", "Justificación");
            grid.Columns["Monto"].DefaultCellStyle.Format = "C2";

            Controls.Add(grid); Controls.Add(filter);
            LoadGrid(0, null);
            cmbMes.SelectedIndexChanged += (s, e) => LoadGrid(cmbMes.SelectedIndex, cmbAnio.SelectedItem is int a ? a : (int?)null);
            cmbAnio.SelectedIndexChanged += (s, e) => LoadGrid(cmbMes.SelectedIndex, cmbAnio.SelectedItem is int a ? a : (int?)null);
        }

        private void LoadGrid(int mesIdx, int? anio)
        {
            grid.Rows.Clear();
            decimal aprobado = 0, rechazado = 0;
            foreach (var op in Repository.GetOperaciones())
            {
                if (op.Resultado == "pendiente") continue;
                if (mesIdx > 0 && DateTime.TryParse(op.Fecha, out var fd) && fd.Month != mesIdx) continue;
                if (anio.HasValue && DateTime.TryParse(op.Fecha, out var fd2) && fd2.Year != anio) continue;
                int i = grid.Rows.Add($"#{op.Id}", op.Fecha, op.Descripcion, op.Monto, op.Resultado.ToUpper(), op.Justificacion);
                grid.Rows[i].DefaultCellStyle.BackColor = op.Resultado == "aprobada" ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
                if (op.Resultado == "aprobada") aprobado += op.Monto; else rechazado += op.Monto;
            }
            lblStats.Text = $"✅ Aprobado: {aprobado:C2}   ❌ Rechazado: {rechazado:C2}";
        }
    }
}
