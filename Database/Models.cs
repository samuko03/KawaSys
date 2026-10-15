namespace KawaSysWF.Database
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Rol { get; set; } = "";
        public bool Activo { get; set; } = true;
        public int? SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "";
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }

    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Ciudad { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public bool Activa { get; set; } = true;
        public override string ToString() => Nombre;
    }

    public class Venta
    {
        public int Id { get; set; }
        public string Fecha { get; set; } = "";
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "";
        public int? ClienteId { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = "";
        public string Observaciones { get; set; } = "";
        /// <summary>N° de factura emitida para esta venta (null si todavía no fue facturada).</summary>
        public int? NroFactura { get; set; }
    }

    public class ModeloFaltante
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int CantidadSolicitada { get; set; }
        public string FechaGeneracion { get; set; } = "";
        public string Periodo { get; set; } = "";
        public bool Emitido { get; set; }
        public string Observaciones { get; set; } = "";
    }

    public class PedidoCliente
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Cantidad { get; set; }
        public string Fecha { get; set; } = "";
        public string Observaciones { get; set; } = "";
    }

    public class EnvioListado
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "";
        public string FechaEnvio { get; set; } = "";
        public string Periodo { get; set; } = "";
        public int CantidadItems { get; set; }
        public string Observaciones { get; set; } = "";
    }

    public class Proveedor
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = "";
        public string Cuit { get; set; } = "";
        public string Tipo { get; set; } = "fijo";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Marcas { get; set; } = "";
        public string Modelos { get; set; } = "";
        public bool Activo { get; set; } = true;
        public override string ToString() => RazonSocial;
    }

    public class SolicitudCompra
    {
        public int Id { get; set; }
        public string Fecha { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Estado { get; set; } = "pendiente";
        public string Items { get; set; } = "";
        public override string ToString() => $"SC-{Id:D3} | {Fecha} | {Descripcion}";
    }

    public class ItemSolicitud
    {
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Cantidad { get; set; } = 1;
    }

    public class SolicitudCotizacion
    {
        public int Id { get; set; }
        public int SolicitudCompraId { get; set; }
        public string Fecha { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Estado { get; set; } = "enviada";
        public override string ToString() => $"SC-{Id:D3} | {Fecha}";
    }

    public class Cotizacion
    {
        public int Id { get; set; }
        public int SolicitudCotizacionId { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public string Fecha { get; set; } = "";
        public decimal MontoTotal { get; set; }
        public string Condiciones { get; set; } = "";
        public string PlazoEntrega { get; set; } = "";
        public bool Seleccionada { get; set; }
        public override string ToString() => $"COT-{Id:D3} | {ProveedorNombre} | ${MontoTotal:N2}";
    }

    public class OrdenCompra
    {
        public int Id { get; set; }
        public int CotizacionId { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public string FechaEmision { get; set; } = "";
        public string Estado { get; set; } = "pendiente";
        public decimal MontoTotal { get; set; }
        public string Observaciones { get; set; } = "";
        public override string ToString() => $"OC-{Id:D3} | {ProveedorNombre} | ${MontoTotal:N2} | {Estado}";
    }

    public class Operacion
    {
        public int Id { get; set; }
        public int? OrdenId { get; set; }
        public string Descripcion { get; set; } = "";
        public decimal Monto { get; set; }
        public string Resultado { get; set; } = "pendiente";
        public string Justificacion { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string FechaResolucion { get; set; } = "";
    }

    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string DniCuit { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Direccion { get; set; } = "";
        public decimal Saldo { get; set; }
        public decimal LimiteCredito { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public override string ToString() => NombreCompleto;
    }

    public class Factura
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int? ClienteId { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string SucursalNombre { get; set; } = "";
        public int NroFactura { get; set; }
        public string Fecha { get; set; } = "";
        public string Tipo { get; set; } = "B";
        public decimal Total { get; set; }
        public decimal Cobrado { get; set; }
        public string Estado { get; set; } = "emitida";
        public bool ACuenta { get; set; }
        public string ClienteDoc { get; set; } = "";
        public string ClienteDireccion { get; set; } = "";
        public string ClienteEmail { get; set; } = "";
        public string FormaPago { get; set; } = "";
        public decimal Pendiente => Total - Cobrado;
        public string NroTexto => $"{NroFactura:D8}";
    }

    public class FacturaLinea
    {
        public string Descripcion { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class Cobro
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public int NroFactura { get; set; }
        public string Fecha { get; set; } = "";
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "";
        public string Observaciones { get; set; } = "";
        public string NroRecibo => $"{Id:D8}";
    }
}
