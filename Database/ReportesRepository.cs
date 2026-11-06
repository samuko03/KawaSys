using System;
using System.Collections.Generic;

namespace KawaSysWF.Database
{
    /// <summary>Reportes estadísticos de facturación y cobranzas (datos para los gráficos).</summary>
    public static class ReportesRepository
    {
        public class Serie
        {
            public string Etiqueta { get; set; } = "";
            public decimal Valor { get; set; }
            public int Cantidad { get; set; }
        }

        public class FilaMes
        {
            public int Mes { get; set; }
            public decimal Facturado { get; set; }
            public decimal Cobrado { get; set; }
        }

        /// <summary>Facturado (por fecha de factura) y cobrado (por fecha de cobro) en cada mes del año.</summary>
        public static List<FilaMes> FacturadoVsCobradoPorMes(int anio)
        {
            var filas = new List<FilaMes>();
            for (int m = 1; m <= 12; m++) filas.Add(new FilaMes { Mes = m });

            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@a", anio.ToString());
            cmd.CommandText = "SELECT CAST(strftime('%m',fecha) AS INTEGER), SUM(total) FROM facturas WHERE strftime('%Y',fecha)=@a GROUP BY 1";
            using (var r = cmd.ExecuteReader())
                while (r.Read()) filas[r.GetInt32(0) - 1].Facturado = (decimal)r.GetDouble(1);
            cmd.CommandText = "SELECT CAST(strftime('%m',fecha) AS INTEGER), SUM(monto) FROM cobros WHERE strftime('%Y',fecha)=@a GROUP BY 1";
            using (var r = cmd.ExecuteReader())
                while (r.Read()) filas[r.GetInt32(0) - 1].Cobrado = (decimal)r.GetDouble(1);
            return filas;
        }

        /// <summary>Monto cobrado por medio de pago en el año, de mayor a menor.</summary>
        public static List<Serie> CobrosPorMedioDePago(int anio) => Series(@"
            SELECT medio_pago, SUM(monto), COUNT(*) FROM cobros
            WHERE strftime('%Y',fecha)=@a GROUP BY medio_pago ORDER BY 2 DESC", anio);

        /// <summary>Cantidad e importe de las facturas del año según su estado (cobrada, parcial, emitida).</summary>
        public static List<Serie> FacturasPorEstado(int anio) => Series(@"
            SELECT estado, SUM(total), COUNT(*) FROM facturas
            WHERE strftime('%Y',fecha)=@a GROUP BY estado ORDER BY 3 DESC", anio);

        /// <summary>Saldo pendiente de cobro por cliente (facturas del año), de mayor a menor.</summary>
        public static List<Serie> DeudaPorCliente(int anio, int top = 5) => Series($@"
            SELECT COALESCE(TRIM(c.nombre||' '||COALESCE(c.apellido,'')),'Consumidor final'),
                   SUM(f.total - COALESCE((SELECT SUM(monto) FROM cobros WHERE factura_id=f.id),0)), COUNT(*)
            FROM facturas f LEFT JOIN clientes c ON c.id=f.cliente_id
            WHERE f.estado IN ('emitida','parcial') AND strftime('%Y',f.fecha)=@a
            GROUP BY f.cliente_id ORDER BY 2 DESC LIMIT {top}", anio);

        /// <summary>Total facturado por sucursal en el año, de mayor a menor.</summary>
        public static List<Serie> FacturadoPorSucursal(int anio) => Series(@"
            SELECT COALESCE(s.nombre,'Sin sucursal'), SUM(f.total), COUNT(*)
            FROM facturas f LEFT JOIN ventas v ON v.id=f.venta_id LEFT JOIN sucursales s ON s.id=v.sucursal_id
            WHERE strftime('%Y',f.fecha)=@a GROUP BY 1 ORDER BY 2 DESC", anio);

        /// <summary>Años que tienen facturas, del más reciente al más antiguo.</summary>
        public static List<int> AniosConFacturas()
        {
            var list = new List<int>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT CAST(strftime('%Y',fecha) AS INTEGER) FROM facturas ORDER BY 1 DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetInt32(0));
            return list;
        }

        private static List<Serie> Series(string sql, int anio)
        {
            var list = new List<Serie>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@a", anio.ToString());
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Serie { Etiqueta = r.GetString(0), Valor = (decimal)r.GetDouble(1), Cantidad = r.GetInt32(2) });
            return list;
        }
    }
}
