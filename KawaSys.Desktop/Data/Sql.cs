using Microsoft.Data.SqlClient;

namespace KawaSys.Desktop.Data;

internal static class Sql
{
    public static int Exec(SqlConnection cn, SqlTransaction tx, string sql, params (string, object?)[] p)
    {
        using var cmd = Cmd(cn, tx, sql, p);
        return cmd.ExecuteNonQuery();
    }

    public static T Scalar<T>(SqlConnection cn, SqlTransaction tx, string sql, params (string, object?)[] p)
    {
        using var cmd = Cmd(cn, tx, sql, p);
        return (T)cmd.ExecuteScalar()!;
    }

    public static List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params (string, object?)[] p)
    {
        using var cn = Db.Open();
        using var cmd = Cmd(cn, null, sql, p);
        using var rd = cmd.ExecuteReader();
        var lista = new List<T>();
        while (rd.Read()) lista.Add(map(rd));
        return lista;
    }

    public static List<T> Query<T>(SqlConnection cn, SqlTransaction tx, string sql, Func<SqlDataReader, T> map, params (string, object?)[] p)
    {
        using var cmd = Cmd(cn, tx, sql, p);
        using var rd = cmd.ExecuteReader();
        var lista = new List<T>();
        while (rd.Read()) lista.Add(map(rd));
        return lista;
    }

    public static SqlCommand Cmd(SqlConnection cn, SqlTransaction? tx, string sql, (string, object?)[] p)
    {
        var cmd = new SqlCommand(sql, cn, tx);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }
}
