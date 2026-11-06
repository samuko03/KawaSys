using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace KawaSysWF.UI
{
    /// <summary>
    /// Gráficos dibujados con GDI+ (sin librerías externas). Cada método dibuja sobre un Graphics y devuelve
    /// las zonas de cada barra con su texto, para mostrar el tooltip al pasar el mouse.
    /// </summary>
    public static class Graficos
    {
        public record Zona(RectangleF Rect, string Texto);
        public record Barra(string Etiqueta, decimal Valor, string Detalle, Color? Color = null);
        public record Serie(string Nombre, Color Color, decimal[] Valores);

        // Paleta categórica en orden fijo (validada para daltonismo) y tinta de texto/ejes.
        public static readonly Color Serie1 = ColorTranslator.FromHtml("#2a78d6");
        public static readonly Color Serie2 = ColorTranslator.FromHtml("#eb6834");
        private static readonly Color Tinta = AppTheme.TextDark;
        private static readonly Color TintaSuave = AppTheme.TextMuted;
        private static readonly Color Grilla = Color.FromArgb(232, 232, 238);
        private static readonly Font Fuente = new("Segoe UI", 8.5f);
        private static readonly Font FuenteValor = new("Segoe UI", 8.5f, FontStyle.Bold);

        private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

        public static string Moneda(decimal v) => v.ToString("C0", Ar);

        /// <summary>$ 3,85 M · $ 450 k — para ejes y etiquetas cortas.</summary>
        public static string Corto(decimal v) =>
            Math.Abs(v) >= 1_000_000 ? "$ " + (v / 1_000_000).ToString("0.##", Ar) + " M"
            : Math.Abs(v) >= 1_000 ? "$ " + (v / 1_000).ToString("0", Ar) + " k"
            : "$ " + v.ToString("0", Ar);

        private static void Preparar(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>Rectángulo con las esquinas del extremo del dato redondeadas (el lado de la base queda recto).</summary>
        private static GraphicsPath BarraRedondeada(RectangleF r, bool vertical, float radio = 4)
        {
            var p = new GraphicsPath();
            radio = Math.Min(radio, Math.Min(r.Width, r.Height) / 2);
            if (radio < 1) { p.AddRectangle(r); return p; }
            float d = radio * 2;
            if (vertical)
            {
                p.AddLine(r.Left, r.Bottom, r.Left, r.Top + radio);
                p.AddArc(r.Left, r.Top, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
                p.AddLine(r.Right, r.Top + radio, r.Right, r.Bottom);
            }
            else
            {
                p.AddLine(r.Left, r.Top, r.Right - radio, r.Top);
                p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddLine(r.Right - radio, r.Bottom, r.Left, r.Bottom);
            }
            p.CloseFigure();
            return p;
        }

        /// <summary>Escala "redonda" del eje: 1, 2 o 5 × 10^n.</summary>
        private static decimal Paso(decimal max, int marcas)
        {
            if (max <= 0) return 1;
            double bruto = (double)max / marcas;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
            double n = bruto / mag;
            return (decimal)((n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * mag);
        }

        /// <summary>Barras verticales agrupadas por categoría (ej. facturado y cobrado de cada mes), con leyenda arriba.</summary>
        public static List<Zona> BarrasAgrupadas(Graphics g, Rectangle area, string[] categorias, IReadOnlyList<Serie> series)
        {
            Preparar(g);
            var zonas = new List<Zona>();

            // Leyenda
            float lx = area.Left;
            foreach (var s in series)
            {
                using var b = new SolidBrush(s.Color);
                g.FillRectangle(b, lx, area.Top + 4, 10, 10);
                using var t = new SolidBrush(Tinta);
                g.DrawString(s.Nombre, Fuente, t, lx + 14, area.Top + 1);
                lx += 14 + g.MeasureString(s.Nombre, Fuente).Width + 16;
            }

            decimal max = series.SelectMany(s => s.Valores).DefaultIfEmpty(0).Max();
            decimal paso = Paso(max, 4);
            decimal tope = Math.Max(paso, Math.Ceiling(max / paso) * paso);

            float ejeAncho = 62;
            var plot = new RectangleF(area.Left + ejeAncho, area.Top + 28, area.Width - ejeAncho - 4, area.Height - 28 - 22);

            using (var pen = new Pen(Grilla))
            using (var tb = new SolidBrush(TintaSuave))
            {
                for (decimal v = 0; v <= tope; v += paso)
                {
                    float y = plot.Bottom - (float)(v / tope) * plot.Height;
                    g.DrawLine(pen, plot.Left, y, plot.Right, y);
                    var txt = Corto(v);
                    var sz = g.MeasureString(txt, Fuente);
                    g.DrawString(txt, Fuente, tb, plot.Left - sz.Width - 6, y - sz.Height / 2);
                }
            }

            float grupo = plot.Width / categorias.Length;
            float hueco = 2;
            float anchoBarra = Math.Min(22, (grupo * 0.7f - hueco * (series.Count - 1)) / series.Count);
            float anchoGrupo = anchoBarra * series.Count + hueco * (series.Count - 1);

            using var tc = new SolidBrush(TintaSuave);
            for (int i = 0; i < categorias.Length; i++)
            {
                float x0 = plot.Left + grupo * i + (grupo - anchoGrupo) / 2;
                for (int k = 0; k < series.Count; k++)
                {
                    decimal v = series[k].Valores[i];
                    var col = new RectangleF(x0 + k * (anchoBarra + hueco), plot.Top, anchoBarra, plot.Height);
                    if (v > 0)
                    {
                        float h = (float)(v / tope) * plot.Height;
                        var r = new RectangleF(col.Left, plot.Bottom - h, anchoBarra, h);
                        using var b = new SolidBrush(series[k].Color);
                        using var path = BarraRedondeada(r, vertical: true);
                        g.FillPath(b, path);
                    }
                    // La zona del tooltip cubre toda la columna: más fácil de apuntar que la barra.
                    zonas.Add(new Zona(col, $"{categorias[i]} · {series[k].Nombre}: {Moneda(v)}"));
                }
                var cat = categorias[i];
                var sc = g.MeasureString(cat, Fuente);
                g.DrawString(cat, Fuente, tc, plot.Left + grupo * i + (grupo - sc.Width) / 2, plot.Bottom + 4);
            }
            using (var ejePen = new Pen(Color.FromArgb(200, 200, 210)))
                g.DrawLine(ejePen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
            return zonas;
        }

        /// <summary>Barras horizontales de una sola serie, ordenadas como vienen, con el valor escrito al final de cada barra.</summary>
        public static List<Zona> BarrasHorizontales(Graphics g, Rectangle area, IReadOnlyList<Barra> barras, Func<decimal, string> formato)
        {
            Preparar(g);
            var zonas = new List<Zona>();
            using var tinta = new SolidBrush(Tinta);
            if (barras.Count == 0)
            {
                using var ts = new SolidBrush(TintaSuave);
                g.DrawString("Sin datos para el período elegido.", Fuente, ts, area.Left, area.Top + 8);
                return zonas;
            }

            float etiquetaAncho = Math.Min(area.Width * 0.38f, barras.Max(b => g.MeasureString(b.Etiqueta, Fuente).Width) + 8);
            float valorAncho = barras.Max(b => g.MeasureString(formato(b.Valor), FuenteValor).Width) + 8;
            float filaAlto = Math.Min(34, area.Height / (float)barras.Count);
            float barraAlto = Math.Min(18, filaAlto - 8);
            float largoMax = area.Width - etiquetaAncho - valorAncho;
            decimal max = Math.Max(1, barras.Max(b => b.Valor));

            for (int i = 0; i < barras.Count; i++)
            {
                var b = barras[i];
                float y = area.Top + i * filaAlto;
                float yBarra = y + (filaAlto - barraAlto) / 2;
                var et = g.MeasureString(b.Etiqueta, Fuente);
                g.DrawString(b.Etiqueta, Fuente, tinta, new RectangleF(area.Left, y + (filaAlto - et.Height) / 2, etiquetaAncho - 6, et.Height + 2),
                             new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                float largo = Math.Max(2, (float)(b.Valor / max) * largoMax);
                var r = new RectangleF(area.Left + etiquetaAncho, yBarra, largo, barraAlto);
                using (var br = new SolidBrush(b.Color ?? Serie1))
                using (var path = BarraRedondeada(r, vertical: false))
                    g.FillPath(br, path);

                var txt = formato(b.Valor);
                var tv = g.MeasureString(txt, FuenteValor);
                g.DrawString(txt, FuenteValor, tinta, r.Right + 4, yBarra + (barraAlto - tv.Height) / 2);

                zonas.Add(new Zona(new RectangleF(area.Left, y, area.Width, filaAlto), $"{b.Etiqueta}: {b.Detalle}"));
            }
            return zonas;
        }
    }

    /// <summary>Panel que dibuja un gráfico y muestra un tooltip sobre la barra que está debajo del mouse.</summary>
    public class PanelGrafico : Panel
    {
        private readonly Func<Graphics, Rectangle, List<Graficos.Zona>> _dibujar;
        private readonly ToolTip _tip = new() { InitialDelay = 0, ReshowDelay = 0 };
        private List<Graficos.Zona> _zonas = new();
        private Graficos.Zona? _actual;

        public PanelGrafico(Func<Graphics, Rectangle, List<Graficos.Zona>> dibujar)
        {
            _dibujar = dibujar;
            DoubleBuffered = true;
            BackColor = AppTheme.White;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var area = new Rectangle(14, 10, Math.Max(10, Width - 28), Math.Max(10, Height - 20));
            _zonas = _dibujar(e.Graphics, area);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var zona = _zonas.FirstOrDefault(z => z.Rect.Contains(e.Location));
            if (zona == _actual) return;
            _actual = zona;
            if (zona == null) _tip.Hide(this);
            else _tip.Show(zona.Texto, this, e.X + 14, e.Y + 14);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _actual = null;
            _tip.Hide(this);
        }
    }
}
