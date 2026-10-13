using System.Diagnostics;
using System.Net;
using System.Text;

namespace KawaSys.Desktop.Data;

/// <summary>Genera comprobantes HTML imprimibles y los abre en el navegador.</summary>
public static class Comprobante
{
    private const string Estilo = @"<style>
        body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#222}
        h1{margin:0}.sub{color:#666;margin-bottom:16px}
        table{border-collapse:collapse;width:100%;margin-top:12px}
        th,td{border:1px solid #ccc;padding:6px 10px;text-align:left}
        th{background:#f0f0f0}.n{text-align:right}.tot{font-size:1.2em;font-weight:bold;text-align:right;margin-top:12px}
        </style>";

    private static string H(object? o) => WebUtility.HtmlEncode(o?.ToString() ?? "");
    private static string M(decimal d) => d.ToString("N2");

    public static void AbrirFactura(FacturaRow f, IReadOnlyList<FacturaLinea> lineas)
    {
        var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
        sb.Append($"<h1>KawaSys - Factura {H(f.Tipo)}</h1><div class='sub'>N° {f.NroFactura:D8} · {f.Fecha:dd/MM/yyyy HH:mm} · Estado: {H(f.Estado)}</div>");
        sb.Append($"<p><b>Cliente:</b> {H(f.Cliente)}<br><b>Sucursal:</b> {H(f.Sucursal)}</p>");
        sb.Append("<table><tr><th>Descripción</th><th class='n'>Cant.</th><th class='n'>Precio unit.</th><th class='n'>Subtotal</th></tr>");
        foreach (var l in lineas)
            sb.Append($"<tr><td>{H(l.Descripcion)}</td><td class='n'>{l.Cantidad}</td><td class='n'>${M(l.PrecioUnitario)}</td><td class='n'>${M(l.SubTotal)}</td></tr>");
        sb.Append($"</table><div class='tot'>Total: ${M(f.Total)}</div>");
        sb.Append($"<p>Cobrado: ${M(f.Cobrado)} · Pendiente: ${M(f.Pendiente)}</p></body></html>");
        Abrir($"factura-{f.NroFactura:D8}.html", sb.ToString());
    }

    public static void AbrirRecibo(CobroRow c, FacturaRow f)
    {
        var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
        sb.Append($"<h1>KawaSys - Recibo de pago</h1><div class='sub'>Recibo N° {c.IdCobro:D8} · {c.Fecha:dd/MM/yyyy HH:mm}</div>");
        sb.Append($"<p>Recibimos de <b>{H(f.Cliente)}</b> la suma de <b>${M(c.Monto)}</b> en concepto de pago de la factura N° {f.NroFactura:D8}.</p>");
        sb.Append($"<p><b>Medio de pago:</b> {H(c.MedioPago)}<br><b>Total de la factura:</b> ${M(f.Total)}<br>");
        sb.Append($"<b>Saldo pendiente:</b> ${M(f.Pendiente)}</p></body></html>");
        Abrir($"recibo-{c.IdCobro:D8}.html", sb.ToString());
    }

    public static void AbrirListadoMensual(int anio, int mes, IReadOnlyList<FacturaRow> filas)
    {
        var sb = new StringBuilder($"<html><head><meta charset='utf-8'>{Estilo}</head><body>");
        sb.Append($"<h1>KawaSys - Facturas {mes:D2}/{anio}</h1><div class='sub'>Cobradas y pendientes</div>");
        sb.Append("<table><tr><th>N°</th><th>Fecha</th><th>Cliente</th><th>Sucursal</th><th>Estado</th><th class='n'>Total</th><th class='n'>Cobrado</th><th class='n'>Pendiente</th></tr>");
        foreach (var f in filas)
            sb.Append($"<tr><td>{f.NroFactura:D8}</td><td>{f.Fecha:dd/MM/yyyy}</td><td>{H(f.Cliente)}</td><td>{H(f.Sucursal)}</td><td>{H(f.Estado)}</td><td class='n'>${M(f.Total)}</td><td class='n'>${M(f.Cobrado)}</td><td class='n'>${M(f.Pendiente)}</td></tr>");
        sb.Append($"</table><div class='tot'>Total facturado: ${M(filas.Sum(f => f.Total))}<br>Cobrado: ${M(filas.Sum(f => f.Cobrado))}<br>Pendiente: ${M(filas.Sum(f => f.Pendiente))}</div></body></html>");
        Abrir($"listado-{anio}-{mes:D2}.html", sb.ToString());
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
