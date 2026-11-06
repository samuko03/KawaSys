using KawaSysWF.Database;

namespace KawaSysWF.Tests.Support
{
    /// <summary>Estado compartido entre los pasos de un escenario (Reqnroll lo inyecta por constructor).</summary>
    public class Contexto
    {
        public int? FacturaId { get; set; }
        public int? FacturaAnteriorId { get; set; }
        public int? VentaId { get; set; }
        public int FacturasGeneradas { get; set; }
        public List<Factura> Facturas { get; set; } = new();
        public List<ReportesRepository.FilaMes> ReporteMensual { get; set; } = new();
        public List<ReportesRepository.Serie> Reporte { get; set; } = new();
        public string? Imagen { get; set; }

        /// <summary>Error que devolvió el sistema en el último "Cuando".</summary>
        public Exception? Error { get; set; }
        public bool ErrorVerificado { get; set; }

        public int Factura => FacturaId ?? throw new InvalidOperationException("El escenario no tiene una factura de referencia.");
        public int Venta => VentaId ?? throw new InvalidOperationException("El escenario no tiene una venta de referencia.");

        /// <summary>Ejecuta una acción del usuario y guarda el error, si lo hay, para que lo verifique un "Entonces".</summary>
        public void Intentar(Action accion)
        {
            Error = null;
            ErrorVerificado = false;
            try { accion(); }
            catch (InvalidOperationException ex) { Error = ex; }
        }
    }
}
