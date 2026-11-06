using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using KawaSysWF.Database;
using KawaSysWF.Forms.Facturacion;

namespace KawaSysWF.UI
{
    /// <summary>Datos y dibujo de los reportes estadísticos de facturación y cobranzas de un año.</summary>
    public class ReporteEstadistico
    {
        public static readonly string[] Meses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        public int Anio { get; private init; }
        public decimal Facturado { get; private init; }
        public decimal Cobrado { get; private init; }
        public decimal Pendiente => Facturado - Cobrado;
        public int CantidadFacturas { get; private init; }
        public List<ReportesRepository.FilaMes> PorMes { get; private init; } = new();
        public List<ReportesRepository.Serie> PorMedio { get; private init; } = new();
        public List<ReportesRepository.Serie> PorEstado { get; private init; } = new();
        public List<ReportesRepository.Serie> Deuda { get; private init; } = new();
        public List<ReportesRepository.Serie> PorSucursal { get; private init; } = new();

        public static ReporteEstadistico Cargar(int anio)
        {
            var facturas = FacturacionRepository.Listar(desde: $"{anio}-01-01", hasta: $"{anio}-12-31");
            return new ReporteEstadistico
            {
                Anio = anio,
                Facturado = facturas.Sum(f => f.Total),
                Cobrado = facturas.Sum(f => f.Cobrado),
                CantidadFacturas = facturas.Count,
                PorMes = ReportesRepository.FacturadoVsCobradoPorMes(anio),
                PorMedio = ReportesRepository.CobrosPorMedioDePago(anio),
                PorEstado = ReportesRepository.FacturasPorEstado(anio),
                Deuda = ReportesRepository.DeudaPorCliente(anio),
                PorSucursal = ReportesRepository.FacturadoPorSucursal(anio),
            };
        }

        public string PorcentajeCobrado => Facturado == 0 ? "—" : $"{Cobrado / Facturado:P0}";

        // ── Un método por gráfico (los usa la pantalla y la exportación a imagen) ──
        public List<Graficos.Zona> DibujarMensual(Graphics g, Rectangle r) =>
            Graficos.BarrasAgrupadas(g, r, Meses, new[]
            {
                new Graficos.Serie("Facturado", Graficos.Serie1, PorMes.Select(m => m.Facturado).ToArray()),
                new Graficos.Serie("Cobrado", Graficos.Serie2, PorMes.Select(m => m.Cobrado).ToArray()),
            });

        public List<Graficos.Zona> DibujarMedios(Graphics g, Rectangle r) =>
            Graficos.BarrasHorizontales(g, r, PorMedio.Select(s =>
                new Graficos.Barra(s.Etiqueta, s.Valor, $"{Graficos.Moneda(s.Valor)} en {s.Cantidad} cobros")).ToList(), Graficos.Corto);

        public List<Graficos.Zona> DibujarEstados(Graphics g, Rectangle r) =>
            Graficos.BarrasHorizontales(g, r, PorEstado.Select(s =>
                new Graficos.Barra(FacturasForm.Cap(s.Etiqueta), s.Cantidad, $"{s.Cantidad} facturas por {Graficos.Moneda(s.Valor)}",
                                   FacturasForm.ColorEstado(s.Etiqueta))).ToList(), v => $"{v:0}");

        public List<Graficos.Zona> DibujarDeuda(Graphics g, Rectangle r) =>
            Graficos.BarrasHorizontales(g, r, Deuda.Select(s =>
                new Graficos.Barra(s.Etiqueta, s.Valor, $"debe {Graficos.Moneda(s.Valor)} en {s.Cantidad} facturas")).ToList(), Graficos.Corto);

        public List<Graficos.Zona> DibujarSucursales(Graphics g, Rectangle r) =>
            Graficos.BarrasHorizontales(g, r, PorSucursal.Select(s =>
                new Graficos.Barra(s.Etiqueta, s.Valor, $"{Graficos.Moneda(s.Valor)} en {s.Cantidad} facturas")).ToList(), Graficos.Corto);

        public (string Titulo, string Valor, Color Color)[] Indicadores => new[]
        {
            ("Facturado", Graficos.Moneda(Facturado), AppTheme.Info),
            ("Cobrado", Graficos.Moneda(Cobrado), AppTheme.Success),
            ("Pendiente de cobro", Graficos.Moneda(Pendiente), AppTheme.Warning),
            ("% cobrado", PorcentajeCobrado, AppTheme.Primary),
            ("Facturas emitidas", CantidadFacturas.ToString(), AppTheme.Accent),
        };

        public (string Titulo, Func<Graphics, Rectangle, List<Graficos.Zona>> Dibujar)[] Graficos4 => new (string, Func<Graphics, Rectangle, List<Graficos.Zona>>)[]
        {
            ("Cobros por medio de pago", DibujarMedios),
            ("Facturas por estado", DibujarEstados),
            ("Clientes con mayor deuda", DibujarDeuda),
            ("Facturado por sucursal", DibujarSucursales),
        };

        /// <summary>Arma una sola imagen con todo el reporte (para imprimir, adjuntar o guardar como evidencia).</summary>
        public Bitmap Imagen()
        {
            const int ancho = 1200, margen = 24, kpiAlto = 70, mensualAlto = 320, chicoAlto = 230, tituloAlto = 30;
            int alto = margen + 40 + kpiAlto + 20 + tituloAlto + mensualAlto + 20 + 2 * (tituloAlto + chicoAlto + 20) + margen;
            var bmp = new Bitmap(ancho, alto);
            using var g = Graphics.FromImage(bmp);
            g.Clear(AppTheme.LightBg);

            using var fTitulo = new Font("Segoe UI", 15, FontStyle.Bold);
            using var fKpi = new Font("Segoe UI", 15, FontStyle.Bold);
            using var tinta = new SolidBrush(AppTheme.Primary);
            using var suave = new SolidBrush(AppTheme.TextMuted);
            using var blanco = new SolidBrush(AppTheme.White);
            using var borde = new Pen(Color.FromArgb(220, 220, 230));

            int y = margen;
            g.DrawString($"KawaSys · Reporte de facturación y cobranzas {Anio}", fTitulo, tinta, margen, y);
            g.DrawString($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", AppTheme.FontSmall, suave, ancho - margen - 170, y + 8);
            y += 40;

            int kpiAncho = (ancho - 2 * margen - 4 * 12) / 5, x = margen;
            foreach (var (titulo, valor, color) in Indicadores)
            {
                g.FillRectangle(blanco, x, y, kpiAncho, kpiAlto);
                g.DrawRectangle(borde, x, y, kpiAncho - 1, kpiAlto - 1);
                using (var b = new SolidBrush(color)) g.FillRectangle(b, x, y, 4, kpiAlto);
                using (var b = new SolidBrush(AppTheme.TextDark)) g.DrawString(valor, fKpi, b, x + 14, y + 8);
                g.DrawString(titulo, AppTheme.FontSmall, suave, x + 14, y + 44);
                x += kpiAncho + 12;
            }
            y += kpiAlto + 20;

            void Tarjeta(string titulo, int cx, int cy, int w, int h, Func<Graphics, Rectangle, List<Graficos.Zona>> dibujar)
            {
                g.FillRectangle(blanco, cx, cy, w, tituloAlto + h);
                g.DrawRectangle(borde, cx, cy, w - 1, tituloAlto + h - 1);
                g.DrawString(titulo, AppTheme.FontBold, tinta, cx + 12, cy + 7);
                dibujar(g, new Rectangle(cx + 14, cy + tituloAlto + 6, w - 28, h - 16));
            }

            Tarjeta("Facturado contra cobrado por mes", margen, y, ancho - 2 * margen, mensualAlto, DibujarMensual);
            y += tituloAlto + mensualAlto + 20;

            int medio = (ancho - 2 * margen - 20) / 2;
            var chicos = Graficos4;
            for (int i = 0; i < chicos.Length; i++)
            {
                int cx = margen + (i % 2) * (medio + 20);
                int cy = y + (i / 2) * (tituloAlto + chicoAlto + 20);
                Tarjeta(chicos[i].Titulo, cx, cy, medio, chicoAlto, chicos[i].Dibujar);
            }
            return bmp;
        }

        public void GuardarImagen(string ruta)
        {
            using var bmp = Imagen();
            bmp.Save(ruta, ImageFormat.Png);
        }
    }
}
