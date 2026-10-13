using KawaSys.Desktop.Models;
using Microsoft.Data.SqlClient;

namespace KawaSys.Desktop.Data;

public record ItemVenta(int IdMoto, string Descripcion, int Cantidad, decimal PrecioUnitario)
{
    public decimal SubTotal => Cantidad * PrecioUnitario;
}

public record ResultadoVenta(int IdVenta, int NroFactura, decimal Total);

public class VentaService
{
    public const string CuentaCorriente = "Cuenta Corriente";

    public List<(int Id, string Nombre)> Clientes() => Listar("SELECT IdCliente, Nombre FROM Cliente WHERE Activo = 1 ORDER BY Nombre");
    public List<(int Id, string Nombre)> Sucursales() => Listar("SELECT IdSucursal, Nombre FROM Sucursal ORDER BY Nombre");

    private static List<(int, string)> Listar(string sql)
    {
        using var cn = Db.Open();
        using var cmd = new SqlCommand(sql, cn);
        using var rd = cmd.ExecuteReader();
        var r = new List<(int, string)>();
        while (rd.Read()) r.Add((rd.GetInt32(0), rd.GetString(1)));
        return r;
    }

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

            int nro = Scalar<int>(cn, tx, "SELECT ISNULL(MAX(NroFactura), 0) + 1 FROM Factura WITH (UPDLOCK, HOLDLOCK)");
            int idFactura = Scalar<int>(cn, tx,
                @"INSERT INTO Factura (IdVenta, IdCliente, NroFactura, Tipo, Total)
                  OUTPUT INSERTED.IdFactura
                  VALUES (@v, @c, @n, 'B', @t)",
                ("@v", idVenta), ("@c", idCliente), ("@n", nro), ("@t", total));

            foreach (var it in items)
                Exec(cn, tx,
                    "INSERT INTO DetalleFactura (IdFactura, Cantidad, PrecioUnitario) VALUES (@f, @q, @p)",
                    ("@f", idFactura), ("@q", it.Cantidad), ("@p", it.PrecioUnitario));

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
                Exec(cn, tx,
                    "INSERT INTO Cobro (IdFactura, Monto, MedioPago) VALUES (@f, @t, @mp)",
                    ("@f", idFactura), ("@t", total), ("@mp", medioPago));
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

    private static int Exec(SqlConnection cn, SqlTransaction tx, string sql, params (string, object)[] p)
    {
        using var cmd = Cmd(cn, tx, sql, p);
        return cmd.ExecuteNonQuery();
    }

    private static T Scalar<T>(SqlConnection cn, SqlTransaction tx, string sql, params (string, object)[] p)
    {
        using var cmd = Cmd(cn, tx, sql, p);
        return (T)cmd.ExecuteScalar()!;
    }

    private static SqlCommand Cmd(SqlConnection cn, SqlTransaction tx, string sql, (string, object)[] p)
    {
        var cmd = new SqlCommand(sql, cn, tx);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return cmd;
    }
}
