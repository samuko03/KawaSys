using Microsoft.Data.SqlClient;
using static KawaSys.Desktop.Data.Sql;

namespace KawaSys.Desktop.Data;

public record ItemVenta(int IdMoto, string Descripcion, int Cantidad, decimal PrecioUnitario)
{
    public decimal SubTotal => Cantidad * PrecioUnitario;
}

public record ResultadoVenta(int IdVenta, int NroFactura, decimal Total);

public class VentaService
{
    public const string CuentaCorriente = "Cuenta Corriente";

    public List<(int Id, string Nombre)> Clientes() => Query("SELECT IdCliente, Nombre FROM Cliente WHERE Activo = 1 ORDER BY Nombre", rd => (rd.GetInt32(0), rd.GetString(1)));
    public List<(int Id, string Nombre)> Sucursales() => Query("SELECT IdSucursal, Nombre FROM Sucursal ORDER BY Nombre", rd => (rd.GetInt32(0), rd.GetString(1)));

    /// <summary>
    /// Transacción central: registra venta + detalle, descuenta stock, emite factura y
    /// registra el cobro (o carga la cuenta corriente). Todo o nada.
    /// </summary>
    public ResultadoVenta Registrar(int idCliente, int idSucursal, string medioPago, IReadOnlyList<ItemVenta> items)
    {
        if (items.Count == 0) throw new InvalidOperationException("La venta no tiene items.");

        using var cn = Db.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            decimal total = items.Sum(i => i.SubTotal);
            bool aCuenta = medioPago == CuentaCorriente;

            int idVenta = Scalar<int>(cn, tx,
                @"INSERT INTO Ventas (IdCliente, IdSucursal, IdEstadoVenta, Total)
                  OUTPUT INSERTED.IdVenta
                  VALUES (@c, @s, (SELECT IdEstadoVenta FROM EstadoVenta WHERE Nombre = @estado), @t)",
                ("@c", idCliente), ("@s", idSucursal), ("@t", total),
                ("@estado", aCuenta ? "Pendiente" : "Pagada"));

            foreach (var it in items)
            {
                // Descuento atómico: falla si el stock no alcanza.
                int filas = Exec(cn, tx,
                    "UPDATE Moto SET Stock = Stock - @q WHERE IdMoto = @m AND Stock >= @q",
                    ("@q", it.Cantidad), ("@m", it.IdMoto));
                if (filas == 0)
                    throw new InvalidOperationException($"Stock insuficiente para {it.Descripcion}.");

                Exec(cn, tx,
                    @"INSERT INTO DetalleVenta (IdVenta, IdMoto, Cantidad, PrecioUnitario)
                      VALUES (@v, @m, @q, @p)",
                    ("@v", idVenta), ("@m", it.IdMoto), ("@q", it.Cantidad), ("@p", it.PrecioUnitario));
            }

            var (idFactura, nro) = FacturaService.GenerarEnTx(cn, tx, idVenta, aCuenta);

            if (aCuenta)
            {
                int filas = Exec(cn, tx,
                    @"UPDATE CuentaCorriente SET Saldo = Saldo + @t
                      WHERE IdCliente = @c AND Saldo + @t <= LimiteCredito",
                    ("@t", total), ("@c", idCliente));
                if (filas == 0)
                    throw new InvalidOperationException("El cliente no tiene cuenta corriente o supera su límite de crédito.");
            }
            else
            {
                FacturaService.CobroEnTx(cn, tx, idFactura, total, medioPago);
            }

            tx.Commit();
            return new ResultadoVenta(idVenta, nro, total);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
