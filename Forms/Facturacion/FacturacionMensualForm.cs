using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Facturacion
{
    /// <summary>Listado mensual de facturas cobradas y pendientes (KAWA-53).</summary>
    public class FacturacionMensualForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbMes = null!;
        private NumericUpDown nudAnio = null!;
        private DataGridView grid = null!;
        private Label lblTotales = null!;
        private List<Factura> _filas = new();

        public FacturacionMensualForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            var filter = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            filter.Controls.Add(new Label { Text = "Mes:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbMes = AppTheme.MakeCombo(140); cmbMes.Left = 10; cmbMes.Top = 32;
            foreach (var m in new[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" })
                cmbMes.Items.Add(m);
            cmbMes.SelectedIndex = DateTime.Today.Month - 1;
            filter.Controls.Add(cmbMes);

            filter.Controls.Add(new Label { Text = "Año:", Left = 165, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            nudAnio = new NumericUpDown { Left = 165, Top = 32, Width = 80, Minimum = 2000, Maximum = 2100, Value = DateTime.Today.Year, Font = AppTheme.FontNormal };
            filter.Controls.Add(nudAnio);

            var btnGenerar = AppTheme.MakeButton("📈 Generar", AppTheme.Accent, 110, 30);
            btnGenerar.Left = 265; btnGenerar.Top = 30; btnGenerar.Click += (s, e) => LoadData();
            var btnEmitir = AppTheme.MakeButton("🖨  Emitir listado", AppTheme.Info, 150, 30);
            btnEmitir.Left = 385; btnEmitir.Top = 30; btnEmitir.Click += (s, e) => Emitir();
            filter.Controls.Add(btnGenerar); filter.Controls.Add(btnEmitir);

            lblTotales = new Label { Left = 555, Top = 36, AutoSize = true, Font = AppTheme.FontBold, ForeColor = AppTheme.Accent };
            filter.Controls.Add(lblTotales);

            grid = AppTheme.MakeGrid();
            grid.Columns.Add("Nro", "N° Factura"); grid.Columns.Add("Fecha", "Fecha");
            grid.Columns.Add("Cliente", "Cliente"); grid.Columns.Add("Suc", "Sucursal");
            grid.Columns.Add("Estado", "Estado"); grid.Columns.Add("Total", "Total");
            grid.Columns.Add("Cobrado", "Cobrado"); grid.Columns.Add("Pend", "Pendiente");
            foreach (var c in new[] { "Total", "Cobrado", "Pend" }) grid.Columns[c].DefaultCellStyle.Format = "C2";

            Controls.Add(grid);
            Controls.Add(filter);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _filas = FacturacionRepository.ListadoMensual((int)nudAnio.Value, cmbMes.SelectedIndex + 1);
                grid.Rows.Clear();
                foreach (var f in _filas)
                {
                    int i = grid.Rows.Add(f.NroTexto, f.Fecha, f.ClienteNombre, f.SucursalNombre, FacturasForm.Cap(f.Estado), f.Total, f.Cobrado, f.Pendiente);
                    grid.Rows[i].Cells["Estado"].Style.ForeColor = FacturasForm.ColorEstado(f.Estado);
                }
                lblTotales.Text = $"Facturado {_filas.Sum(f => f.Total):C2}  ·  Cobrado {_filas.Sum(f => f.Cobrado):C2}  ·  Pendiente {_filas.Sum(f => f.Pendiente):C2}";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Emitir() =>
            Comprobantes.AbrirListadoMensual((int)nudAnio.Value, cmbMes.SelectedIndex + 1, _filas);
    }
}
