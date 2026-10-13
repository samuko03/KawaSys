using KawaSysWF.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace KawaSysWF.UI
{
    /// <summary>Genera comprobantes HTML imprimibles (factura, recibo, listado mensual) y los abre en el navegador.</summary>
    public static class Comprobantes
    {
        private const string Estilo = @"<style>
            body{font-family:'Segoe UI',Arial,sans-serif;margin:32px;color:#1e1e1e}
            .head{background:#1a1a2e;color:#fff;padding:16px 20px;border-bottom:4px solid #e94560}
            .head h1{margin:0;font-size:22px}.head div{color:#b8b8d0;font-size:13px;margin-top:4px}
            table{border-collapse:collapse;width:100%;margin-top:14px}
            th,td{border:1px solid #d8d8e2;padding:7px 10px;text-align:left}
            th{background:#f0f2f5}.n{text-align:right}
            .tot{font-size:1.25em;font-weight:bold;text-align:right;margin-top:14px}
            </style>";

        private static string H(object? o) => WebUtility.HtmlEncode(o?.ToString() ?? "");
        private static string M(decimal d) => d.ToString("C2");
        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

        public static void AbrirFactura(Factura f, IEnumerable<FacturaLinea> lineas)
        {
            var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
            sb.Append($"<div class='head'><h1>KawaSys — Factura {H(f.Tipo)}</h1><div>N° {f.NroTexto} · {H(f.Fecha)} · Estado: {H(Cap(f.Estado))}</div></div>");
            sb.Append($"<p><b>Cliente:</b> {H(f.ClienteNombre)}<br><b>Sucursal:</b> {H(f.SucursalNombre)}</p>");
            sb.Append("<table><tr><th>Descripción</th><th class='n'>Cant.</th><th class='n'>Precio unit.</th><th class='n'>Subtotal</th></tr>");
            foreach (var l in lineas)
                sb.Append($"<tr><td>{H(l.Descripcion)}</td><td class='n'>{l.Cantidad}</td><td class='n'>{M(l.PrecioUnitario)}</td><td class='n'>{M(l.SubTotal)}</td></tr>");
            sb.Append($"</table><div class='tot'>Total: {M(f.Total)}</div>");
            sb.Append($"<p>Cobrado: {M(f.Cobrado)} · Pendiente: {M(f.Pendiente)}</p></body></html>");
            Abrir($"factura-{f.NroTexto}.html", sb.ToString());
        }

        public static void AbrirRecibo(Cobro c, Factura f)
        {
            var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
            sb.Append($"<div class='head'><h1>KawaSys — Recibo de pago</h1><div>Recibo N° {c.NroRecibo} · {H(c.Fecha)}</div></div>");
            sb.Append($"<p>Recibimos de <b>{H(f.ClienteNombre)}</b> la suma de <b>{M(c.Monto)}</b> en concepto de pago de la factura N° {f.NroTexto}.</p>");
            sb.Append($"<p><b>Medio de pago:</b> {H(c.MedioPago)}<br><b>Total de la factura:</b> {M(f.Total)}<br>");
            sb.Append($"<b>Saldo pendiente:</b> {M(f.Pendiente)}</p></body></html>");
            Abrir($"recibo-{c.NroRecibo}.html", sb.ToString());
        }

        public static void AbrirListadoMensual(int anio, int mes, IReadOnlyList<Factura> filas)
        {
            var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
            sb.Append($"<div class='head'><h1>KawaSys — Facturas {mes:D2}/{anio}</h1><div>Cobradas y pendientes</div></div>");
            sb.Append("<table><tr><th>N°</th><th>Fecha</th><th>Cliente</th><th>Sucursal</th><th>Estado</th><th class='n'>Total</th><th class='n'>Cobrado</th><th class='n'>Pendiente</th></tr>");
            foreach (var f in filas)
                sb.Append($"<tr><td>{f.NroTexto}</td><td>{H(f.Fecha)}</td><td>{H(f.ClienteNombre)}</td><td>{H(f.SucursalNombre)}</td><td>{H(Cap(f.Estado))}</td><td class='n'>{M(f.Total)}</td><td class='n'>{M(f.Cobrado)}</td><td class='n'>{M(f.Pendiente)}</td></tr>");
            sb.Append($"</table><div class='tot'>Facturado: {M(filas.Sum(f => f.Total))}<br>Cobrado: {M(filas.Sum(f => f.Cobrado))}<br>Pendiente: {M(filas.Sum(f => f.Pendiente))}</div></body></html>");
            Abrir($"facturas-{anio}-{mes:D2}.html", sb.ToString());
        }

        private static void Abrir(string nombre, string html)
        {
            var dir = Path.Combine(Path.GetTempPath(), "KawaSys");
            Directory.CreateDirectory(dir);
            var ruta = Path.Combine(dir, nombre);
            File.WriteAllText(ruta, html, Encoding.UTF8);
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
    }
}
