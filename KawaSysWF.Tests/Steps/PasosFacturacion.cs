using KawaSysWF.Database;
using KawaSysWF.Tests.Support;
using Reqnroll;
using Xunit;

namespace KawaSysWF.Tests.Steps
{
    /// <summary>Pasos de facturación, cobros y ventas facturadas (KAWA-43 a KAWA-51).</summary>
    [Binding]
    public class PasosFacturacion
    {
        private const int SucursalNorte = 2;
        private readonly Contexto _ctx;
        public PasosFacturacion(Contexto ctx) => _ctx = ctx;

        private Factura FacturaActual => FacturacionRepository.Obtener(_ctx.Factura)
            ?? throw new InvalidOperationException($"No existe la factura {_ctx.Factura}.");

        private Venta? VentaActual => Repository.GetVentas().SingleOrDefault(v => v.Id == _ctx.Venta);

        private void RegistrarVenta(int? clienteId, int cantidad, string marca, string modelo, decimal precio, string formaPago)
        {
            var venta = new Venta
            {
                Fecha = DateTime.Today.ToString("yyyy-MM-dd"), SucursalId = SucursalNorte, ClienteId = clienteId,
                Marca = marca, Modelo = modelo, Cantidad = cantidad, PrecioUnitario = precio,
                Total = cantidad * precio, FormaPago = formaPago
            };
            _ctx.FacturaAnteriorId = _ctx.FacturaId;
            _ctx.FacturaId = Repository.SaveVenta(venta);
            _ctx.VentaId = FacturaActual.VentaId;
        }

        // ── Ventas que generan factura ──────────────────────────────────────
        [When(@"^registro una venta a consumidor final de (\d+) ""([^""]*)"" ""([^""]*)"" a (\d+) cada una con forma de pago ""([^""]*)""$")]
        public void VentaConsumidorFinal(int cantidad, string marca, string modelo, decimal precio, string formaPago) =>
            RegistrarVenta(null, cantidad, marca, modelo, precio, formaPago);

        [When(@"^registro una venta al cliente ""([^""]*)"" de (\d+) ""([^""]*)"" ""([^""]*)"" a (\d+) cada una con forma de pago ""([^""]*)""$")]
        public void VentaCliente(string cliente, int cantidad, string marca, string modelo, decimal precio, string formaPago) =>
            RegistrarVenta(PasosComunes.Cliente(cliente).Id, cantidad, marca, modelo, precio, formaPago);

        [Given(@"^una factura en cuenta corriente de ""([^""]*)"" por (\d+)$")]
        public void FacturaEnCuentaCorriente(string cliente, decimal monto) =>
            RegistrarVenta(PasosComunes.Cliente(cliente).Id, 1, "Honda", "XR150", monto, FacturacionRepository.CuentaCorriente);

        [Given(@"^una venta facturada de (\d+) ""([^""]*)"" ""([^""]*)"" a (\d+)$")]
        public void VentaFacturada(int cantidad, string marca, string modelo, decimal precio) =>
            RegistrarVenta(null, cantidad, marca, modelo, precio, "Efectivo");

        [Given(@"^la factura de la venta histórica número (\d+)$")]
        public void FacturaDeVentaHistorica(int ventaId)
        {
            _ctx.VentaId = ventaId;
            _ctx.FacturaId = FacturacionRepository.Listar().Single(f => f.VentaId == ventaId).Id;
        }

        [When(@"^facturo las ventas pendientes de facturar$")]
        public void FacturarPendientes() => _ctx.FacturasGeneradas = FacturacionRepository.GenerarPendientes();

        // ── Estado de la factura ────────────────────────────────────────────
        [Then(@"^se genera una factura en estado ""([^""]*)""$")]
        [Then(@"^la factura queda en estado ""([^""]*)""$")]
        public void EstadoFactura(string estado) => Assert.Equal(estado, FacturaActual.Estado);

        [Then(@"^la factura tiene un total de (\d+)$")]
        public void TotalFactura(decimal total) => Assert.Equal(total, FacturaActual.Total);

        [Then(@"^el saldo pendiente de la factura es (\d+)$")]
        public void PendienteFactura(decimal pendiente) => Assert.Equal(pendiente, FacturaActual.Pendiente);

        [Then(@"^la factura tiene un cobro de (\d+) con medio de pago ""([^""]*)""$")]
        public void CobroUnico(decimal monto, string medio)
        {
            var cobro = Assert.Single(FacturacionRepository.Cobros(_ctx.Factura));
            Assert.Equal(monto, cobro.Monto);
            Assert.Equal(medio, cobro.MedioPago);
        }

        [Then(@"^la factura no tiene cobros registrados$")]
        public void SinCobros() => Assert.Empty(FacturacionRepository.Cobros(_ctx.Factura));

        [Then(@"^la factura tiene (\d+) cobros registrados$")]
        public void CantidadCobros(int cantidad) => Assert.Equal(cantidad, FacturacionRepository.Cobros(_ctx.Factura).Count);

        [Then(@"^el detalle de la factura tiene (\d+) líneas?$")]
        public void LineasDetalle(int lineas) => Assert.Equal(lineas, FacturacionRepository.Detalle(_ctx.Factura).Count);

        [Then(@"^la línea de la factura es (\d+) x ""([^""]*)"" a (\d+) con subtotal (\d+)$")]
        public void LineaDetalle(int cantidad, string descripcion, decimal precio, decimal subtotal)
        {
            var linea = Assert.Single(FacturacionRepository.Detalle(_ctx.Factura));
            Assert.Equal(cantidad, linea.Cantidad);
            Assert.Equal(descripcion, linea.Descripcion);
            Assert.Equal(precio, linea.PrecioUnitario);
            Assert.Equal(subtotal, linea.SubTotal);
        }

        [Then(@"^la factura está a nombre de ""([^""]*)""$")]
        public void NombreFactura(string cliente) => Assert.Equal(cliente, FacturaActual.ClienteNombre);

        [Then(@"^la última factura tiene el número siguiente a la anterior$")]
        public void Correlativas()
        {
            var anterior = FacturacionRepository.Obtener(_ctx.FacturaAnteriorId!.Value)!;
            Assert.Equal(anterior.NroFactura + 1, FacturaActual.NroFactura);
        }

        [Then(@"^se generan (\d+) facturas$")]
        public void FacturasGeneradas(int cantidad) => Assert.Equal(cantidad, _ctx.FacturasGeneradas);

        [Then(@"^no queda ninguna venta sin factura$")]
        public void TodasFacturadas() => Assert.All(Repository.GetVentas(), v => Assert.NotNull(v.NroFactura));

        // ── Cobros ──────────────────────────────────────────────────────────
        [When(@"^registro un cobro de (-?\d+) con ""([^""]*)""$")]
        public void RegistrarCobro(decimal monto, string medio) =>
            _ctx.Intentar(() => FacturacionRepository.RegistrarCobro(_ctx.Factura, monto, medio));

        [When(@"^registro un cobro de (-?\d+) con ""([^""]*)"" sobre la factura (\d+)$")]
        public void RegistrarCobroSobre(decimal monto, string medio, int facturaId) =>
            _ctx.Intentar(() => FacturacionRepository.RegistrarCobro(facturaId, monto, medio));

        // ── Ventas facturadas (KAWA-51) ─────────────────────────────────────
        [Given(@"^la venta histórica número (\d+), que no tiene factura$")]
        public void VentaSinFactura(int ventaId)
        {
            _ctx.VentaId = ventaId;
            Assert.NotNull(VentaActual);
            Assert.Null(FacturacionRepository.FacturaDeVenta(ventaId));
        }

        [When(@"^intento cambiar la cantidad de la venta a (\d+)$")]
        public void CambiarCantidad(int cantidad)
        {
            var venta = VentaActual!;
            venta.Cantidad = cantidad;
            venta.Total = cantidad * venta.PrecioUnitario;
            _ctx.Intentar(() => Repository.UpdateVenta(venta));
        }

        [When(@"^intento eliminar la venta$")]
        public void EliminarVenta() => _ctx.Intentar(() => Repository.DeleteVenta(_ctx.Venta));

        [Then(@"^la venta sigue teniendo cantidad (\d+) y total (\d+)$")]
        public void VentaSinCambios(int cantidad, decimal total)
        {
            var venta = VentaActual!;
            Assert.Equal(cantidad, venta.Cantidad);
            Assert.Equal(total, venta.Total);
        }

        [Then(@"^la venta sigue existiendo$")]
        public void VentaExiste() => Assert.NotNull(VentaActual);

        [Then(@"^la venta ya no existe$")]
        public void VentaNoExiste() => Assert.Null(VentaActual);
    }
}
