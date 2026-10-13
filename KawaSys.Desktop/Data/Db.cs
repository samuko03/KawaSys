using Microsoft.Data.SqlClient;

namespace KawaSys.Desktop.Data;

public static class Db
{
    // Se puede sobreescribir con la variable de entorno KAWASYS_CONNECTION.
    private const string DefaultConnection =
        @"Server=localhost\MSQLSAMUKO;Database=KawaSys;Integrated Security=true;TrustServerCertificate=true";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("KAWASYS_CONNECTION") ?? DefaultConnection;

    public static SqlConnection Open()
    {
        var cn = new SqlConnection(ConnectionString);
        cn.Open();
        return cn;
    }
}
