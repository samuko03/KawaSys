using Microsoft.Data.SqlClient;

namespace KawaSys.Desktop.Data;

public record FacturaRow(int IdFactura, int NroFactura, DateTime Fecha, string Cliente, string Sucursal,
                         string Tipo, decimal Total, decimal Cobrado, string Estado)
{
    public decimal Pendiente => Total - Cobrado;
}

public record FacturaLinea(string Descripcion, int Cantidad, decimal PrecioUnitario, decimal SubTotal);

public record CobroRow(int IdCobro, int IdFactura, int NroFactura, DateTime Fecha, decimal Monto, string MedioPago);

public record FiltroFacturas(int? IdCliente = null, int? IdSucursal = null, DateTime? Desde = null,
                             DateTime? Hasta = null, bool SoloPendientes = false);

public class FacturaService
{
    public static readonly string[] MediosDePago = ["Efectivo", "Tarjeta", "Transferencia", "Cheque"];

    // ---------- KAWA-43 / KAWA-44: generar factura y su detalle ----------

    /// <summary>Genera la factura (cabecera + detalle) de una venta dentro de la transacción recibida.</summary>
    internal static (int IdFactura, int NroFactura) GenerarEnTx(SqlConnection cn, SqlTransaction tx, int idVenta, bool aCuenta)
    {
        var (idCliente, total) = Sql.Query(cn, tx, "SELECT IdCliente, Total FROM Ventas WHERE IdVenta = @v",
            rd => (rd.GetInt32(0), rd.GetDecimal(1)), ("@v", idVenta)).Single();

        int nro = Sql.Scalar<int>(cn, tx, "SELECT ISNULL(MAX(NroFactura), 0) + 1 FROM Factura WITH (UPDLOCK, HOLDLOCK)");
        int idFactura = Sql.Scalar<int>(cn, tx,
            @"INSERT INTO Factura (IdVenta, IdCliente, NroFactura, Tipo, Total, ACuenta)
              OUTPUT INSERTED.IdFactura
              VALUES (@v, @c, @n, 'B', @t, @a)",
            ("@v", idVenta), ("@c", idCliente), ("@n", nro), ("@t", total), ("@a", aCuenta));

        Sql.Exec(cn, tx,
            @"INSERT INTO DetalleFactura (IdFactura, IdMoto, Descripcion, Cantidad, PrecioUnitario)
              SELECT @f, d.IdMoto, m.Marca + ' ' + m.Modelo + ' ' + m.Color, d.Cantidad, d.PrecioUnitario
              FROM DetalleVenta d JOIN Moto m ON m.IdMoto = d.IdMoto WHERE d.IdVenta = @v",
            ("@f", idFactura), ("@v", idVenta));

        return (idFactura, nro);
    }

    /// <summary>Genera las facturas de las ventas que todavía no tienen una. Devuelve cuántas creó.</summary>
    public int GenerarPendientes()
    {
        using var cn = Db.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            var ventas = Sql.Query(cn, tx,
                @"SELECT v.IdVenta FROM Ventas v
                  JOIN EstadoVenta e ON e.IdEstadoVenta = v.IdEstadoVenta
                  WHERE e.Nombre <> 'Anulada' AND NOT EXISTS (SELECT 1 FROM Factura f WHERE f.IdVenta = v.IdVenta)",
                rd => rd.GetInt32(0));
            foreach (var id in ventas) GenerarEnTx(cn, tx, id, aCuenta: false);
            tx.Commit();
            return ventas.Count;
        }
        catch { tx.Rollback(); throw; }
    }

    // ---------- KAWA-46 / KAWA-50 / KAWA-52: consultas ----------

    public List<FacturaRow> Listar(FiltroFacturas f) => Sql.Query(
        @"SELECT f.IdFactura, f.NroFactura, f.Fecha, c.Nombre, s.Nombre, f.Tipo, f.Total,
                 ISNULL((SELECT SUM(Monto) FROM Cobro WHERE IdFactura = f.IdFactura), 0), f.Estado
          FROM Factura f
          JOIN Cliente c ON c.IdCliente = f.IdCliente
          JOIN Ventas v ON v.IdVenta = f.IdVenta
          JOIN Sucursal s ON s.IdSucursal = v.IdSucursal
          WHERE (@cli IS NULL OR f.IdCliente = @cli)
            AND (@suc IS NULL OR v.IdSucursal = @suc)
            AND (@desde IS NULL OR f.Fecha >= @desde)
            AND (@hasta IS NULL OR f.Fecha < @hasta)
            AND (@pend = 0 OR f.Estado IN ('Emitida', 'Parcial'))
          ORDER BY f.Fecha DESC, f.NroFactura DESC",
        rd => new FacturaRow(rd.GetInt32(0), rd.GetInt32(1), rd.GetDateTime(2), rd.GetString(3), rd.GetString(4),
                             rd.GetString(5), rd.GetDecimal(6), rd.GetDecimal(7), rd.GetString(8)),
        ("@cli", f.IdCliente), ("@suc", f.IdSucursal),
        ("@desde", f.Desde?.Date), ("@hasta", f.Hasta?.Date.AddDays(1)),
        ("@pend", f.SoloPendientes ? 1 : 0));

    public List<FacturaLinea> Detalle(int idFactura) => Sql.Query(
        "SELECT Descripcion, Cantidad, PrecioUnitario, SubTotal FROM DetalleFactura WHERE IdFactura = @f ORDER BY IdDetalleFactura",
        rd => new FacturaLinea(rd.GetString(0), rd.GetInt32(1), rd.GetDecimal(2), rd.GetDecimal(3)),
        ("@f", idFactura));

    public List<CobroRow> Cobros(int idFactura) => Sql.Query(
        @"SELECT c.IdCobro, c.IdFactura, f.NroFactura, c.Fecha, c.Monto, c.MedioPago
          FROM Cobro c JOIN Factura f ON f.IdFactura = c.IdFactura
          WHERE c.IdFactura = @f ORDER BY c.Fecha",
        rd => new CobroRow(rd.GetInt32(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetDateTime(3), rd.GetDecimal(4), rd.GetString(5)),
        ("@f", idFactura));

    public FacturaRow? Obtener(int idFactura) => Sql.Query(
        @"SELECT f.IdFactura, f.NroFactura, f.Fecha, c.Nombre, s.Nombre, f.Tipo, f.Total,
                 ISNULL((SELECT SUM(Monto) FROM Cobro WHERE IdFactura = f.IdFactura), 0), f.Estado
          FROM Factura f
          JOIN Cliente c ON c.IdCliente = f.IdCliente
          JOIN Ventas v ON v.IdVenta = f.IdVenta
          JOIN Sucursal s ON s.IdSucursal = v.IdSucursal
          WHERE f.IdFactura = @f",
        rd => new FacturaRow(rd.GetInt32(0), rd.GetInt32(1), rd.GetDateTime(2), rd.GetString(3), rd.GetString(4),
                             rd.GetString(5), rd.GetDecimal(6), rd.GetDecimal(7), rd.GetString(8)),
        ("@f", idFactura)).SingleOrDefault();

    // ---------- KAWA-53: listado mensual ----------

    public List<FacturaRow> ListadoMensual(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        return Listar(new FiltroFacturas(Desde: desde, Hasta: desde.AddMonths(1).AddDays(-1)))
            .Where(f => f.Estado != "Anulada").ToList();
    }

    // ---------- KAWA-47 / KAWA-48 / KAWA-51: cobros y estado de factura ----------

    private record DatosFactura(decimal Total, string Estado, bool ACuenta, int IdVenta, int IdCliente, decimal Cobrado);

    private static DatosFactura LeerFactura(SqlConnection cn, SqlTransaction tx, int idFactura) =>
        Sql.Query(cn, tx,
            @"SELECT f.Total, f.Estado, f.ACuenta, f.IdVenta, f.IdCliente,
                     ISNULL((SELECT SUM(Monto) FROM Cobro WHERE IdFactura = f.IdFactura), 0)
              FROM Factura f WITH (UPDLOCK) WHERE f.IdFactura = @f",
            rd => new DatosFactura(rd.GetDecimal(0), rd.GetString(1), rd.GetBoolean(2), rd.GetInt32(3), rd.GetInt32(4), rd.GetDecimal(5)),
            ("@f", idFactura)).SingleOrDefault() ?? throw new InvalidOperationException("La factura no existe.");

    /// <summary>
    /// Registra un cobro, actualiza el estado de la factura (Emitida -> Parcial -> Cobrada)
    /// y, si la factura estaba a cuenta corriente, descuenta el saldo del cliente.
    /// </summary>
    internal static int CobroEnTx(SqlConnection cn, SqlTransaction tx, int idFactura, decimal monto, string medioPago)
    {
        if (!MediosDePago.Contains(medioPago))
            throw new InvalidOperationException("Medio de pago inválido.");
        if (monto <= 0)
            throw new InvalidOperationException("El monto debe ser mayor a cero.");

        var f = LeerFactura(cn, tx, idFactura);
        if (f.Estado == "Anulada") throw new InvalidOperationException("La factura está anulada.");
        decimal pendiente = f.Total - f.Cobrado;
        if (monto > pendiente)
            throw new InvalidOperationException($"El monto supera el saldo pendiente ({pendiente:N2}).");

        int idCobro = Sql.Scalar<int>(cn, tx,
            @"INSERT INTO Cobro (IdFactura, Monto, MedioPago) OUTPUT INSERTED.IdCobro VALUES (@f, @m, @mp)",
            ("@f", idFactura), ("@m", monto), ("@mp", medioPago));

        bool cobradaTotal = f.Cobrado + monto >= f.Total;
        Sql.Exec(cn, tx, "UPDATE Factura SET Estado = @e WHERE IdFactura = @f",
            ("@e", cobradaTotal ? "Cobrada" : "Parcial"), ("@f", idFactura));

        if (cobradaTotal)
            Sql.Exec(cn, tx,
                "UPDATE Ventas SET IdEstadoVenta = (SELECT IdEstadoVenta FROM EstadoVenta WHERE Nombre = 'Pagada') WHERE IdVenta = @v",
                ("@v", f.IdVenta));

        if (f.ACuenta)
            Sql.Exec(cn, tx, "UPDATE CuentaCorriente SET Saldo = Saldo - @m WHERE IdCliente = @c",
                ("@m", monto), ("@c", f.IdCliente));

        return idCobro;
    }

    public int RegistrarCobro(int idFactura, decimal monto, string medioPago)
    {
        using var cn = Db.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            int id = CobroEnTx(cn, tx, idFactura, monto, medioPago);
            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>Anula una factura sin cobros: libera el saldo de cuenta corriente y devuelve el stock.</summary>
    public void Anular(int idFactura)
    {
        using var cn = Db.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            var f = LeerFactura(cn, tx, idFactura);
            if (f.Estado == "Anulada") throw new InvalidOperationException("La factura ya está anulada.");
            if (f.Cobrado > 0) throw new InvalidOperationException("La factura tiene cobros registrados y no puede anularse.");

            Sql.Exec(cn, tx, "UPDATE Factura SET Estado = 'Anulada' WHERE IdFactura = @f", ("@f", idFactura));
            Sql.Exec(cn, tx,
                "UPDATE Ventas SET IdEstadoVenta = (SELECT IdEstadoVenta FROM EstadoVenta WHERE Nombre = 'Anulada') WHERE IdVenta = @v",
                ("@v", f.IdVenta));
            Sql.Exec(cn, tx,
                @"UPDATE m SET m.Stock = m.Stock + d.Cantidad
                  FROM Moto m JOIN DetalleVenta d ON d.IdMoto = m.IdMoto WHERE d.IdVenta = @v",
                ("@v", f.IdVenta));
            if (f.ACuenta)
                Sql.Exec(cn, tx, "UPDATE CuentaCorriente SET Saldo = Saldo - @t WHERE IdCliente = @c",
                    ("@t", f.Total), ("@c", f.IdCliente));
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }
}
