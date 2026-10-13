using KawaSys.Desktop.Models;
using Microsoft.Data.SqlClient;

namespace KawaSys.Desktop.Data;

public class MotoRepository
{
    public List<Moto> Buscar(string? texto = null)
    {
        const string sql = @"SELECT IdMoto, Modelo, Marca, Precio, Stock, Cilindrada, Color
                             FROM Moto
                             WHERE Activo = 1 AND (@t IS NULL OR Modelo LIKE @t OR Marca LIKE @t)
                             ORDER BY Marca, Modelo";
        using var cn = Db.Open();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(texto) ? DBNull.Value : $"%{texto.Trim()}%");
        using var rd = cmd.ExecuteReader();
        var lista = new List<Moto>();
        while (rd.Read())
        {
            lista.Add(new Moto
            {
                IdMoto = rd.GetInt32(0),
                Modelo = rd.GetString(1),
                Marca = rd.GetString(2),
                Precio = rd.GetDecimal(3),
                Stock = rd.GetInt32(4),
                Cilindrada = rd.GetInt32(5),
                Color = rd.GetString(6)
            });
        }
        return lista;
    }

    public void Alta(Moto m)
    {
        const string sql = @"INSERT INTO Moto (Modelo, Marca, Precio, Stock, Cilindrada, Color)
                             VALUES (@modelo, @marca, @precio, @stock, @cil, @color)";
        Ejecutar(sql, m);
    }

    public void Modificar(Moto m)
    {
        const string sql = @"UPDATE Moto SET Modelo=@modelo, Marca=@marca, Precio=@precio,
                             Stock=@stock, Cilindrada=@cil, Color=@color WHERE IdMoto=@id";
        Ejecutar(sql, m);
    }

    // Baja lógica: se conserva el historial de ventas y compras.
    public void Baja(int idMoto)
    {
        using var cn = Db.Open();
        using var cmd = new SqlCommand("UPDATE Moto SET Activo = 0 WHERE IdMoto = @id", cn);
        cmd.Parameters.AddWithValue("@id", idMoto);
        cmd.ExecuteNonQuery();
    }

    private static void Ejecutar(string sql, Moto m)
    {
        using var cn = Db.Open();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", m.IdMoto);
        cmd.Parameters.AddWithValue("@modelo", m.Modelo);
        cmd.Parameters.AddWithValue("@marca", m.Marca);
        cmd.Parameters.AddWithValue("@precio", m.Precio);
        cmd.Parameters.AddWithValue("@stock", m.Stock);
        cmd.Parameters.AddWithValue("@cil", m.Cilindrada);
        cmd.Parameters.AddWithValue("@color", m.Color);
        cmd.ExecuteNonQuery();
    }
}
