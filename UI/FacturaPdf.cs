using KawaSysWF.Database;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace KawaSysWF.UI
{
    /// <summary>Genera la factura en PDF (cabecera con logo, datos del cliente, detalle, totales y pagos).</summary>
    public static class FacturaPdf
    {
        private static readonly CultureInfo AR = new CultureInfo("es-AR");
        private const string Navy = "#1A1A2E", Accent = "#E94560", Muted = "#6B7280", Light = "#F3F4F8", Line = "#D9DBE6";
        private const string Ok = "#16A34A", Warn = "#F97316", Info = "#3B82F6", Gris = "#6B7280";

        static FacturaPdf() { QuestPDF.Settings.License = LicenseType.Community; }

        private static string M(decimal d) => d.ToString("C2", AR);

        private static string Fecha(string iso) =>
            DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd/MM/yyyy", AR) : iso;

        private static (string texto, string color) Estado(string estado) => estado switch
        {
            "cobrada" => ("COBRADA", Ok),
            "parcial" => ("COBRO PARCIAL", Warn),
            "anulada" => ("ANULADA", Gris),
            _ => ("PENDIENTE DE COBRO", Info)
        };

        private static byte[]? Logo()
        {
            try
            {
                var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                return File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
            }
            catch { return null; }
        }

        public static IDocument Crear(Factura f, IReadOnlyList<FacturaLinea> lineas, IReadOnlyList<Cobro> cobros)
        {
            var logo = Logo();
            var (estadoTxt, estadoColor) = Estado(f.Estado);

            return Document.Create(doc => doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Navy));

                // ── Cabecera ──────────────────────────────────────────────
                page.Header().Column(h =>
                {
                    h.Item().Background(Navy).Padding(24).Row(row =>
                    {
                        if (logo != null)
                            row.ConstantItem(74).Height(58).Background(Colors.White).Padding(4).Image(logo).FitArea();
                        row.RelativeItem().PaddingLeft(logo != null ? 14 : 0).AlignMiddle().Column(c =>
                        {
                            c.Item().Text("KawaSys").FontSize(24).Bold().FontColor(Colors.White);
                            c.Item().Text("KAWA S.R.L. · Venta y gestión de motos").FontSize(9).FontColor("#B8B8D0");
                        });
                        row.ConstantItem(190).AlignRight().AlignMiddle().Column(c =>
                        {
                            c.Item().AlignRight().Text($"FACTURA  {f.Tipo}").FontSize(11).Bold().FontColor("#F5A3B2");
                            c.Item().AlignRight().Text($"N° {f.NroTexto}").FontSize(20).Bold().FontColor(Colors.White);
                            c.Item().AlignRight().Text($"Fecha: {Fecha(f.Fecha)}").FontSize(9).FontColor("#B8B8D0");
                        });
                    });
                    h.Item().Height(4).Background(Accent);
                });

                // ── Contenido ─────────────────────────────────────────────
                page.Content().Padding(28).Column(col =>
                {
                    col.Spacing(18);

                    // Cliente + operación
                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Background(Light).Padding(14).Column(c =>
                        {
                            c.Spacing(3);
                            c.Item().Text("CLIENTE").FontSize(8).Bold().FontColor(Accent);
                            c.Item().Text(f.ClienteNombre).FontSize(13).Bold();
                            if (!string.IsNullOrWhiteSpace(f.ClienteDoc)) c.Item().Text(t => { t.Span("CUIT/DNI: ").FontColor(Muted); t.Span(f.ClienteDoc); });
                            if (!string.IsNullOrWhiteSpace(f.ClienteDireccion)) c.Item().Text(t => { t.Span("Dirección: ").FontColor(Muted); t.Span(f.ClienteDireccion); });
                            if (!string.IsNullOrWhiteSpace(f.ClienteEmail)) c.Item().Text(t => { t.Span("Email: ").FontColor(Muted); t.Span(f.ClienteEmail); });
                        });
                        row.RelativeItem().Background(Light).Padding(14).Column(c =>
                        {
                            c.Spacing(3);
                            c.Item().Text("DATOS DE LA OPERACIÓN").FontSize(8).Bold().FontColor(Accent);
                            c.Item().Text(t => { t.Span("Sucursal: ").FontColor(Muted); t.Span(f.SucursalNombre).Bold(); });
                            if (!string.IsNullOrWhiteSpace(f.FormaPago)) c.Item().Text(t => { t.Span("Forma de pago: ").FontColor(Muted); t.Span(f.FormaPago).Bold(); });
                            c.Item().Text(t => { t.Span("Estado: ").FontColor(Muted); t.Span(estadoTxt).Bold().FontColor(estadoColor); });
                        });
                    });

                    // Detalle
                    col.Item().Column(c =>
                    {
                        c.Spacing(6);
                        c.Item().Text("DETALLE").FontSize(8).Bold().FontColor(Accent);
                        c.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(28);
                                cols.RelativeColumn(5);
                                cols.ConstantColumn(52);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(Th).Text("#");
                                h.Cell().Element(Th).Text("Descripción");
                                h.Cell().Element(Th).AlignRight().Text("Cant.");
                                h.Cell().Element(Th).AlignRight().Text("Precio unit.");
                                h.Cell().Element(Th).AlignRight().Text("Subtotal");
                            });
                            for (int i = 0; i < lineas.Count; i++)
                            {
                                var l = lineas[i]; bool alt = i % 2 == 1;
                                table.Cell().Element(x => Td(x, alt)).Text((i + 1).ToString()).FontColor(Muted);
                                table.Cell().Element(x => Td(x, alt)).Text(l.Descripcion).Bold();
                                table.Cell().Element(x => Td(x, alt)).AlignRight().Text(l.Cantidad.ToString());
                                table.Cell().Element(x => Td(x, alt)).AlignRight().Text(M(l.PrecioUnitario));
                                table.Cell().Element(x => Td(x, alt)).AlignRight().Text(M(l.SubTotal));
                            }
                        });
                    });

                    // Totales
                    col.Item().AlignRight().Width(250).Column(c =>
                    {
                        c.Spacing(0);
                        c.Item().Background(Navy).Padding(12).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").FontSize(12).Bold().FontColor(Colors.White);
                            r.AutoItem().Text(M(f.Total)).FontSize(15).Bold().FontColor(Colors.White);
                        });
                        c.Item().Background(Light).PaddingHorizontal(12).PaddingVertical(7).Row(r =>
                        {
                            r.RelativeItem().Text("Cobrado").FontColor(Muted);
                            r.AutoItem().Text(M(f.Cobrado)).Bold().FontColor(Ok);
                        });
                        c.Item().Background(Light).PaddingHorizontal(12).PaddingBottom(8).Row(r =>
                        {
                            r.RelativeItem().Text("Pendiente").FontColor(Muted);
                            r.AutoItem().Text(M(f.Pendiente)).Bold().FontColor(f.Pendiente > 0 ? Accent : Ok);
                        });
                        c.Item().PaddingTop(8).AlignRight().Text(estadoTxt).FontSize(11).Bold().FontColor(estadoColor);
                    });

                    // Pagos registrados
                    if (cobros.Count > 0)
                        col.Item().Column(c =>
                        {
                            c.Spacing(6);
                            c.Item().Text("PAGOS REGISTRADOS").FontSize(8).Bold().FontColor(Accent);
                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(3); cols.RelativeColumn(2); });
                                table.Header(h =>
                                {
                                    h.Cell().Element(Th).Text("Recibo N°");
                                    h.Cell().Element(Th).Text("Fecha");
                                    h.Cell().Element(Th).Text("Medio de pago");
                                    h.Cell().Element(Th).AlignRight().Text("Monto");
                                });
                                for (int i = 0; i < cobros.Count; i++)
                                {
                                    var p = cobros[i]; bool alt = i % 2 == 1;
                                    table.Cell().Element(x => Td(x, alt)).Text(p.NroRecibo);
                                    table.Cell().Element(x => Td(x, alt)).Text(Fecha(p.Fecha));
                                    table.Cell().Element(x => Td(x, alt)).Text(p.MedioPago);
                                    table.Cell().Element(x => Td(x, alt)).AlignRight().Text(M(p.Monto)).Bold();
                                }
                            });
                        });
                });

                // ── Pie ───────────────────────────────────────────────────
                page.Footer().Column(fc =>
                {
                    fc.Item().Height(1).Background(Line);
                    fc.Item().PaddingHorizontal(28).PaddingVertical(10).Row(r =>
                    {
                        r.RelativeItem().Text($"Comprobante generado por KawaSys el {DateTime.Now.ToString("dd/MM/yyyy HH:mm", AR)}")
                            .FontSize(8).FontColor(Muted);
                        r.AutoItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                            t.Span("Página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages();
                        });
                    });
                });
            }));
        }

        private static IContainer Th(IContainer c) =>
            c.Background(Navy).PaddingVertical(7).PaddingHorizontal(8)
             .DefaultTextStyle(t => t.FontColor(Colors.White).Bold().FontSize(9));

        private static IContainer Td(IContainer c, bool alt) =>
            c.Background(alt ? Light : Colors.White).BorderBottom(0.6f).BorderColor(Line).PaddingVertical(8).PaddingHorizontal(8);

        // ── Archivo ───────────────────────────────────────────────────────────
        /// <summary>Carpeta donde se guardan las facturas: Documentos\KawaSys\Facturas.</summary>
        public static string Carpeta()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KawaSys", "Facturas");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Genera el PDF de la factura y devuelve la ruta del archivo.</summary>
        public static string Generar(Factura f, IReadOnlyList<FacturaLinea> lineas, IReadOnlyList<Cobro> cobros)
        {
            var doc = Crear(f, lineas, cobros);
            var ruta = Path.Combine(Carpeta(), $"Factura-{f.NroTexto}.pdf");
            try { doc.GeneratePdf(ruta); }
            catch (IOException)   // el PDF anterior está abierto en un visor: se guarda con otro nombre
            {
                ruta = Path.Combine(Carpeta(), $"Factura-{f.NroTexto}-{DateTime.Now:HHmmss}.pdf");
                doc.GeneratePdf(ruta);
            }
            return ruta;
        }

        /// <summary>Abre el archivo con el programa predeterminado (salvo que KAWA_NO_ABRIR=1, usado en pruebas).</summary>
        public static void Abrir(string ruta)
        {
            if (Environment.GetEnvironmentVariable("KAWA_NO_ABRIR") == "1") return;
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
    }
}
