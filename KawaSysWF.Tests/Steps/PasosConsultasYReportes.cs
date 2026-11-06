using KawaSysWF.Database;
using KawaSysWF.Tests.Support;
using KawaSysWF.UI;
using Reqnroll;
using Xunit;

namespace KawaSysWF.Tests.Steps
{
    /// <summary>Pasos de consultas, listado mensual (KAWA-50/52/53) y reportes estadísticos.</summary>
    [Binding]
    public class PasosConsultasYReportes
    {
        private readonly Contexto _ctx;
        public PasosConsultasYReportes(Contexto ctx) => _ctx = ctx;

        // ── Consultas de facturas ───────────────────────────────────────────
        [When(@"^consulto las facturas pendientes de cobro$")]
        public void Pendientes() => _ctx.Facturas = FacturacionRepository.Listar(soloPendientes: true);

        [When(@"^consulto las facturas del cliente ""([^""]*)""$")]
        public void DelCliente(string cliente) =>
            _ctx.Facturas = FacturacionRepository.Listar(clienteId: PasosComunes.Cliente(cliente).Id);

        [When(@"^genero el listado mensual de (\d+) de (\d+)$")]
        public void ListadoMensual(int mes, int anio) => _ctx.Facturas = FacturacionRepository.ListadoMensual(anio, mes);

        [Then(@"^obtengo (\d+) facturas$")]
        public void CantidadFacturas(int cantidad) => Assert.Equal(cantidad, _ctx.Facturas.Count);

        [Then(@"^todas están en estado ""([^""]*)"" o ""([^""]*)""$")]
        public void TodasEnEstado(string estado1, string estado2) =>
            Assert.All(_ctx.Facturas, f => Assert.Contains(f.Estado, new[] { estado1, estado2 }));

        [Then(@"^todas son del cliente ""([^""]*)""$")]
        public void TodasDelCliente(string cliente) => Assert.All(_ctx.Facturas, f => Assert.Equal(cliente, f.ClienteNombre));

        [Then(@"^el total (facturado|cobrado|pendiente) es (\d+)$")]
        public void Total(string columna, decimal monto) => Assert.Equal(monto, columna switch
        {
            "facturado" => _ctx.Facturas.Sum(f => f.Total),
            "cobrado" => _ctx.Facturas.Sum(f => f.Cobrado),
            _ => _ctx.Facturas.Sum(f => f.Pendiente)
        });

        [Then(@"^las facturas están ordenadas por número de menor a mayor$")]
        public void OrdenadasPorNumero()
        {
            var numeros = _ctx.Facturas.Select(f => f.NroFactura).ToList();
            Assert.NotEmpty(numeros);
            Assert.Equal(numeros.OrderBy(n => n), numeros);
        }

        // ── Reportes estadísticos ───────────────────────────────────────────
        [When(@"^consulto el reporte de facturado contra cobrado de (\d+)$")]
        public void ReporteMensual(int anio) => _ctx.ReporteMensual = ReportesRepository.FacturadoVsCobradoPorMes(anio);

        [When(@"^consulto el reporte de (cobros por medio de pago|facturas por estado|deuda por cliente|facturado por sucursal) de (\d+)$")]
        public void Reporte(string reporte, int anio) => _ctx.Reporte = Consultar(reporte, anio);

        private static List<ReportesRepository.Serie> Consultar(string reporte, int anio) => reporte switch
        {
            "cobros por medio de pago" => ReportesRepository.CobrosPorMedioDePago(anio),
            "facturas por estado" => ReportesRepository.FacturasPorEstado(anio),
            "deuda por cliente" => ReportesRepository.DeudaPorCliente(anio),
            _ => ReportesRepository.FacturadoPorSucursal(anio)
        };

        [Then(@"^el reporte mensual muestra:$")]
        public void ReporteMensualMuestra(Table tabla)
        {
            foreach (var fila in tabla.Rows)
            {
                var mes = _ctx.ReporteMensual[int.Parse(fila["mes"]) - 1];
                Assert.Equal(decimal.Parse(fila["facturado"]), mes.Facturado);
                Assert.Equal(decimal.Parse(fila["cobrado"]), mes.Cobrado);
            }
        }

        [Then(@"^los demás meses figuran en cero$")]
        public void DemasMesesEnCero()
        {
            Assert.Equal(12, _ctx.ReporteMensual.Count);
            Assert.Equal(2, _ctx.ReporteMensual.Count(m => m.Facturado != 0 || m.Cobrado != 0));
        }

        [Then(@"^el reporte muestra, de mayor a menor:$")]
        public void ReporteMuestraValores(Table tabla)
        {
            var esperado = tabla.Rows.Select(f => (f["etiqueta"], decimal.Parse(f["valor"]))).ToList();
            Assert.Equal(esperado, _ctx.Reporte.Select(s => (s.Etiqueta, s.Valor)).ToList());
        }

        [Then(@"^el reporte muestra estas cantidades:$")]
        public void ReporteMuestraCantidades(Table tabla)
        {
            var esperado = tabla.Rows.Select(f => (f["etiqueta"], int.Parse(f["cantidad"]))).ToList();
            Assert.Equal(esperado, _ctx.Reporte.Select(s => (s.Etiqueta, s.Cantidad)).ToList());
        }

        [When(@"^exporto el reporte de (\d+) como imagen ""([^""]*)""$")]
        public void ExportarImagen(int anio, string archivo)
        {
            Directory.CreateDirectory("reportes");
            _ctx.Imagen = Path.GetFullPath(Path.Combine("reportes", archivo));
            ReporteEstadistico.Cargar(anio).GuardarImagen(_ctx.Imagen);
        }

        [Then(@"^se genera una imagen PNG de (\d+) píxeles de ancho$")]
        public void ImagenGenerada(int ancho)
        {
            using var imagen = System.Drawing.Image.FromFile(_ctx.Imagen!);
            Assert.Equal(System.Drawing.Imaging.ImageFormat.Png, imagen.RawFormat);
            Assert.Equal(ancho, imagen.Width);
        }

        [Then(@"^al consultar el reporte de facturas por estado de (\d+) hay (\d+) facturas? en estado ""([^""]*)""$")]
        public void CantidadEnEstado(int anio, int cantidad, string estado) =>
            Assert.Equal(cantidad, ReportesRepository.FacturasPorEstado(anio).SingleOrDefault(s => s.Etiqueta == estado)?.Cantidad ?? 0);
    }
}
