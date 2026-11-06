using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms.Facturacion
{
    /// <summary>Reportes estadísticos con gráficos: facturado, cobrado, medios de pago, estados, deuda y sucursales.</summary>
    public class ReportesForm : Form
    {
        private readonly Usuario _user;
        private ComboBox cmbAnio = null!;
        private FlowLayoutPanel contenido = null!;
        private ReporteEstadistico? _reporte;

        public ReportesForm(Usuario u) { _user = u; Build(); }

        private void Build()
        {
            BackColor = AppTheme.LightBg;

            var filter = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(12, 10, 12, 8) };
            filter.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, filter.Height - 1, filter.Width, filter.Height - 1);

            filter.Controls.Add(new Label { Text = "Año:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontBold });
            cmbAnio = AppTheme.MakeCombo(100); cmbAnio.Left = 10; cmbAnio.Top = 32;
            var anios = ReportesRepository.AniosConFacturas();
            if (!anios.Contains(DateTime.Today.Year)) anios.Insert(0, DateTime.Today.Year);
            foreach (var a in anios.OrderByDescending(a => a)) cmbAnio.Items.Add(a);
            // Arranca en el año más reciente que tenga facturas.
            cmbAnio.SelectedItem = ReportesRepository.AniosConFacturas().DefaultIfEmpty(DateTime.Today.Year).First();
            filter.Controls.Add(cmbAnio);

            var btnActualizar = AppTheme.MakeButton("🔄 Actualizar", AppTheme.Accent, 120, 30);
            btnActualizar.Left = 125; btnActualizar.Top = 30; btnActualizar.Click += (s, e) => LoadData();
            var btnExportar = AppTheme.MakeButton("🖼  Exportar imagen", AppTheme.Info, 160, 30);
            btnExportar.Left = 255; btnExportar.Top = 30; btnExportar.Click += (s, e) => Exportar();
            filter.Controls.Add(btnActualizar); filter.Controls.Add(btnExportar);
            filter.Controls.Add(new Label
            {
                Text = "Pasá el mouse sobre una barra para ver el detalle.", Left = 430, Top = 37, AutoSize = true,
                Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
            });

            contenido = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.LightBg,
                Padding = new Padding(12, 12, 12, 20), FlowDirection = FlowDirection.LeftToRight
            };
            contenido.Resize += (s, e) => Acomodar();

            Controls.Add(contenido);
            Controls.Add(filter);
            cmbAnio.SelectedIndexChanged += (s, e) => LoadData();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _reporte = ReporteEstadistico.Cargar((int)cmbAnio.SelectedItem!);
                contenido.SuspendLayout();
                foreach (Control c in contenido.Controls.Cast<Control>().ToList()) c.Dispose();
                contenido.Controls.Clear();

                foreach (var (titulo, valor, color) in _reporte.Indicadores)
                {
                    var p = new Panel { Width = 190, Height = 76, BackColor = AppTheme.White, Margin = new Padding(0, 0, 10, 12), Tag = "kpi" };
                    p.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 230)), 0, 0, p.Width - 1, p.Height - 1);
                    p.Controls.Add(new Panel { Left = 0, Top = 0, Width = 4, Height = 76, BackColor = color });
                    p.Controls.Add(new Label { Text = valor, Left = 14, Top = 10, AutoSize = true, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = AppTheme.TextDark });
                    p.Controls.Add(new Label { Text = titulo, Left = 14, Top = 46, AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted });
                    contenido.Controls.Add(p);
                }

                contenido.Controls.Add(Tarjeta("Facturado contra cobrado por mes", 330, _reporte.DibujarMensual, ancha: true));
                foreach (var (titulo, dibujar) in _reporte.Graficos4)
                    contenido.Controls.Add(Tarjeta(titulo, 240, dibujar, ancha: false));

                Acomodar();
                contenido.ResumeLayout();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static Panel Tarjeta(string titulo, int alto, Func<Graphics, Rectangle, System.Collections.Generic.List<Graficos.Zona>> dibujar, bool ancha)
        {
            var card = AppTheme.MakeCard(titulo, 400, alto);
            card.Margin = new Padding(0, 0, 12, 12);
            card.Tag = ancha ? "ancha" : "media";
            var grafico = new PanelGrafico(dibujar) { Dock = DockStyle.Fill };
            card.Controls.Add(grafico);
            grafico.BringToFront();
            return card;
        }

        // El gráfico mensual ocupa todo el ancho; los otros cuatro van de a dos por fila.
        private void Acomodar()
        {
            int util = Math.Max(600, contenido.ClientSize.Width - contenido.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
            foreach (Control c in contenido.Controls)
            {
                if (Equals(c.Tag, "ancha")) c.Width = util - 12;
                else if (Equals(c.Tag, "media")) c.Width = (util - 24) / 2;
            }
        }

        private void Exportar()
        {
            if (_reporte == null) return;
            using var dlg = new SaveFileDialog
            {
                Filter = "Imagen PNG|*.png",
                FileName = $"Reporte_Facturacion_{_reporte.Anio}.png"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                _reporte.GuardarImagen(dlg.FileName);
                MessageBox.Show($"Reporte guardado en:\n{dlg.FileName}", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
