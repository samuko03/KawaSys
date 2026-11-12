using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Motos
{
    /// <summary>Gestión de Motos (RG): registrar, modificar, dar de baja, consultar por marca o modelo y actualizar stock.</summary>
    public class MotosForm : Form
    {
        private readonly Usuario _user;
        private DataGridView grid = null!;
        private TextBox txtBuscar = null!;
        private CheckBox chkInactivas = null!;

        public MotosForm(Usuario u) { _user = u; Build(); }

        private static void Error(string msg) =>
            MessageBox.Show(msg, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private Moto? Seleccionada()
        {
            if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Moto m) return m;
            MessageBox.Show("Seleccioná un modelo de moto.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        private void Build()
        {
            BackColor = AppTheme.LightBg;
            var toolbar = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 8, 12, 8) };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);

            var btnNueva = AppTheme.MakeButton("➕  Registrar modelo", AppTheme.Accent, 170, 32);
            var btnEditar = AppTheme.MakeButton("✏  Modificar", AppTheme.Info, 120, 32);
            var btnBaja = AppTheme.MakeButton("🗑  Dar de baja", Color.FromArgb(220, 38, 38), 130, 32);
            var btnStock = AppTheme.MakeButton("📦  Actualizar stock", AppTheme.Success, 160, 32);
            foreach (var b in new[] { btnNueva, btnEditar, btnBaja, btnStock }) b.Margin = new Padding(0, 0, 8, 0);

            btnNueva.Click += (s, e) => { new MotoEditForm(null).ShowDialog(); LoadGrid(); };
            btnEditar.Click += (s, e) => { if (Seleccionada() is Moto m) { new MotoEditForm(m).ShowDialog(); LoadGrid(); } };
            btnBaja.Click += (s, e) =>
            {
                if (Seleccionada() is not Moto m) return;
                if (!m.Activa) { MessageBox.Show($"{m.Descripcion} ya está dado de baja. Podés reactivarlo desde Modificar.", "Info"); return; }
                if (MessageBox.Show($"¿Dar de baja el modelo {m.Descripcion}?\nDejará de ofrecerse en las ventas, pero se conserva en el historial.",
                        "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                MotosRepository.DarDeBaja(m.Id);
                LoadGrid();
            };
            btnStock.Click += (s, e) => { if (Seleccionada() is Moto m) { new StockForm(m).ShowDialog(); LoadGrid(); } };

            var lblBuscar = new Label { Text = "Buscar marca o modelo:", AutoSize = true, Font = AppTheme.FontBold, Margin = new Padding(16, 8, 4, 0) };
            txtBuscar = AppTheme.MakeTextBox(180);
            txtBuscar.Margin = new Padding(0, 2, 8, 0);
            txtBuscar.TextChanged += (s, e) => LoadGrid();
            chkInactivas = new CheckBox { Text = "Mostrar dados de baja", AutoSize = true, Font = AppTheme.FontNormal, Margin = new Padding(4, 7, 0, 0) };
            chkInactivas.CheckedChanged += (s, e) => LoadGrid();

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            flow.Controls.AddRange(new Control[] { btnNueva, btnEditar, btnBaja, btnStock, lblBuscar, txtBuscar, chkInactivas });
            toolbar.Controls.Add(flow);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Marca", "Marca");
            grid.Columns.Add("Modelo", "Modelo");
            grid.Columns.Add("Cilindrada", "Cilindrada");
            grid.Columns.Add("Color", "Color");
            grid.Columns.Add("Precio", "Precio de lista");
            grid.Columns.Add("Stock", "Stock");
            grid.Columns.Add("Estado", "Estado");
            grid.Columns["Precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is Moto m) { new MotoEditForm(m).ShowDialog(); LoadGrid(); }
            };

            Controls.Add(grid);
            Controls.Add(toolbar);
            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var m in MotosRepository.Buscar(txtBuscar.Text, soloActivas: !chkInactivas.Checked))
            {
                int i = grid.Rows.Add(m.Marca, m.Modelo, m.Cilindrada > 0 ? $"{m.Cilindrada} cc" : "", m.Color,
                                      m.Precio.ToString("C0"), m.Stock, m.Activa ? "Activo ✓" : "Dado de baja");
                grid.Rows[i].Tag = m;
                if (!m.Activa) grid.Rows[i].DefaultCellStyle.ForeColor = AppTheme.TextMuted;
                else if (m.Stock == 0) grid.Rows[i].Cells["Stock"].Style.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }
    }

    public class MotoEditForm : Form
    {
        private readonly Moto? _m;
        public MotoEditForm(Moto? m) { _m = m; Build(); }

        private void Build()
        {
            Text = _m == null ? "Registrar modelo de moto" : $"Modificar: {_m.Descripcion}";
            Size = new Size(440, _m == null ? 360 : 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = AppTheme.White;

            var txtMarca = AppTheme.MakeTextBox(180); txtMarca.Text = _m?.Marca ?? "";
            var txtModelo = AppTheme.MakeTextBox(180); txtModelo.Text = _m?.Modelo ?? "";
            var nudCil = new NumericUpDown { Width = 180, Maximum = 3000, Value = _m?.Cilindrada ?? 0, Font = AppTheme.FontNormal };
            var txtColor = AppTheme.MakeTextBox(180); txtColor.Text = _m?.Color ?? "";
            var txtPrecio = AppTheme.MakeTextBox(180); txtPrecio.Text = _m == null ? "" : _m.Precio.ToString("0.##");
            var nudStock = new NumericUpDown { Width = 180, Maximum = 99999, Value = _m?.Stock ?? 0, Font = AppTheme.FontNormal };

            int y = 16;
            void Fila(string l1, Control c1, string l2, Control c2)
            {
                Controls.Add(new Label { Text = l1, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                Controls.Add(new Label { Text = l2, Left = 216, Top = y, AutoSize = true, Font = AppTheme.FontBold });
                c1.Left = 16; c1.Top = y + 20; c2.Left = 216; c2.Top = y + 20;
                Controls.Add(c1); Controls.Add(c2);
                y += 58;
            }
            Fila("Marca *", txtMarca, "Modelo *", txtModelo);
            Fila("Cilindrada (cc)", nudCil, "Color", txtColor);
            Fila("Precio de lista *", txtPrecio, _m == null ? "Stock inicial" : "Stock", nudStock);

            CheckBox? chkActiva = null;
            if (_m != null)
            {
                chkActiva = new CheckBox { Text = "Modelo activo (se ofrece en las ventas)", Checked = _m.Activa, Left = 16, Top = y, AutoSize = true, Font = AppTheme.FontNormal };
                Controls.Add(chkActiva); y += 30;
            }

            var btn = AppTheme.MakeButton("✓  Guardar", AppTheme.Accent, 130, 36);
            btn.Left = 16; btn.Top = y + 6;
            btn.Click += (s, e) =>
            {
                if (!AppTheme.TryParseMonto(txtPrecio.Text, out decimal precio))
                { MessageBox.Show("Ingresá un precio válido (por ejemplo 285000).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var moto = new Moto
                {
                    Id = _m?.Id ?? 0, Marca = txtMarca.Text, Modelo = txtModelo.Text, Cilindrada = (int)nudCil.Value,
                    Color = txtColor.Text, Precio = precio, Stock = (int)nudStock.Value, Activa = chkActiva?.Checked ?? true
                };
                try
                {
                    if (_m == null) MotosRepository.Registrar(moto); else MotosRepository.Modificar(moto);
                    Close();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            Controls.Add(btn);
            AcceptButton = btn;
        }
    }

    /// <summary>Ingreso o ajuste de unidades de un modelo.</summary>
    public class StockForm : Form
    {
        public StockForm(Moto m)
        {
            Text = $"Actualizar stock: {m.Descripcion}";
            Size = new Size(380, 230);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = AppTheme.White;

            Controls.Add(new Label { Text = $"Stock actual: {m.Stock} unidades", Left = 16, Top = 16, AutoSize = true, Font = AppTheme.FontBold });
            Controls.Add(new Label { Text = "Unidades a sumar (ingreso) o restar con signo menos (ajuste):", Left = 16, Top = 48, Width = 340, Height = 34, Font = AppTheme.FontNormal });
            var nud = new NumericUpDown { Left = 16, Top = 86, Width = 120, Minimum = -99999, Maximum = 99999, Font = AppTheme.FontNormal };
            Controls.Add(nud);

            var btn = AppTheme.MakeButton("✓  Actualizar", AppTheme.Accent, 140, 36);
            btn.Left = 16; btn.Top = 124;
            btn.Click += (s, e) =>
            {
                try
                {
                    int nuevo = MotosRepository.ActualizarStock(m.Id, (int)nud.Value);
                    MessageBox.Show($"Stock de {m.Descripcion} actualizado: {nuevo} unidades.", "KawaSys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            Controls.Add(btn);
        }
    }
}
