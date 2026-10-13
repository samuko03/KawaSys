using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KawaSysWF.Database
{
    /// <summary>Facturación y cobranzas (KAWA-43 a KAWA-53).</summary>
    public static class FacturacionRepository
    {
        public static readonly string[] MediosDePago = { "Efectivo", "Transferencia", "Crédito", "Cheque" };
        public const string CuentaCorriente = "Cuenta corriente";

        public static void EnsureSchema(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS facturas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    venta_id INTEGER NOT NULL UNIQUE,
    cliente_id INTEGER,
    nro_factura INTEGER NOT NULL UNIQUE,
    fecha TEXT NOT NULL,
    tipo TEXT DEFAULT 'B',
    total REAL NOT NULL,
    estado TEXT NOT NULL DEFAULT 'emitida',
    a_cuenta INTEGER DEFAULT 0,
    debito_cc INTEGER DEFAULT 0,
    FOREIGN KEY (venta_id) REFERENCES ventas(id),
    FOREIGN KEY (cliente_id) REFERENCES clientes(id)
);

CREATE TABLE IF NOT EXISTS detalle_factura (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    factura_id INTEGER NOT NULL,
    descripcion TEXT NOT NULL,
    cantidad INTEGER NOT NULL,
    precio_unitario REAL NOT NULL,
    subtotal REAL NOT NULL,
    FOREIGN KEY (factura_id) REFERENCES facturas(id)
);

CREATE TABLE IF NOT EXISTS cobros (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    factura_id INTEGER NOT NULL,
    fecha TEXT NOT NULL,
    monto REAL NOT NULL,
    medio_pago TEXT NOT NULL,
    observaciones TEXT,
    FOREIGN KEY (factura_id) REFERENCES facturas(id)
);";
            cmd.ExecuteNonQuery();
        }

        // ── Cuenta corriente dentro de una transacción ────────────────────────
        // (mismo criterio que Repository.RegistrarTransaccionCC: débito resta, crédito suma)
        private static void CcEnTx(SqliteConnection conn, SqliteTransaction tx, int clienteId, string tipo, decimal monto, string descripcion)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT id FROM cuentas_corrientes WHERE cliente_id=@c";
            cmd.Parameters.AddWithValue("@c", clienteId);
            var ccId = (long?)cmd.ExecuteScalar();
            if (ccId == null) return;

            cmd.Parameters.Clear();
            cmd.CommandText = tipo == "credito"
                ? "UPDATE cuentas_corrientes SET saldo=saldo+@m WHERE id=@cc"
                : "UPDATE cuentas_corrientes SET saldo=saldo-@m WHERE id=@cc";
            cmd.Parameters.AddWithValue("@m", (double)monto);
            cmd.Parameters.AddWithValue("@cc", ccId);
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO transacciones_cc(cuenta_id,fecha,tipo,monto,descripcion) VALUES(@cc,@f,@t,@m,@d)";
            cmd.Parameters.AddWithValue("@cc", ccId);
            cmd.Parameters.AddWithValue("@f", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@t", tipo);
            cmd.Parameters.AddWithValue("@m", (double)monto);
            cmd.Parameters.AddWithValue("@d", descripcion);
            cmd.ExecuteNonQuery();
        }

        // ── KAWA-43 / KAWA-44: generar factura y detalle desde la venta ───────
        /// <summary>
        /// Genera la factura de una venta. Si la forma de pago es "Cuenta corriente" queda pendiente
        /// de cobro; en cualquier otro caso se registra el cobro completo y queda cobrada.
        /// </summary>
        internal static int GenerarEnTx(SqliteConnection conn, SqliteTransaction tx, int ventaId, bool debitarCC)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT fecha, cliente_id, total, COALESCE(forma_pago,''), COALESCE(marca,''), modelo, cantidad, precio_unitario
                                FROM ventas WHERE id=@v";
            cmd.Parameters.AddWithValue("@v", ventaId);

            string fecha, formaPago, marca, modelo;
            int? clienteId; double total, precio; int cantidad;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) throw new InvalidOperationException("La venta no existe.");
                fecha = r.GetString(0);
                clienteId = r.IsDBNull(1) ? null : r.GetInt32(1);
                total = r.GetDouble(2);
                formaPago = r.GetString(3);
                marca = r.GetString(4);
                modelo = r.GetString(5);
                cantidad = r.GetInt32(6);
                precio = r.GetDouble(7);
            }

            bool pendiente = formaPago == CuentaCorriente;
            bool aCuenta = pendiente && clienteId != null;
            bool debitar = aCuenta && debitarCC;

            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT COALESCE(MAX(nro_factura),0)+1 FROM facturas";
            int nro = (int)(long)cmd.ExecuteScalar()!;

            cmd.CommandText = @"INSERT INTO facturas(venta_id,cliente_id,nro_factura,fecha,tipo,total,estado,a_cuenta,debito_cc)
                                VALUES(@v,@c,@n,@f,'B',@t,@e,@ac,@d); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@v", ventaId);
            cmd.Parameters.AddWithValue("@c", (object?)clienteId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@n", nro);
            cmd.Parameters.AddWithValue("@f", fecha);
            cmd.Parameters.AddWithValue("@t", total);
            cmd.Parameters.AddWithValue("@e", pendiente ? "emitida" : "cobrada");
            cmd.Parameters.AddWithValue("@ac", aCuenta ? 1 : 0);
            cmd.Parameters.AddWithValue("@d", debitar ? 1 : 0);
            int facturaId = (int)(long)cmd.ExecuteScalar()!;

            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO detalle_factura(factura_id,descripcion,cantidad,precio_unitario,subtotal) VALUES(@f,@d,@c,@p,@s)";
            cmd.Parameters.AddWithValue("@f", facturaId);
            cmd.Parameters.AddWithValue("@d", $"{marca} {modelo}".Trim());
            cmd.Parameters.AddWithValue("@c", cantidad);
            cmd.Parameters.AddWithValue("@p", precio);
            cmd.Parameters.AddWithValue("@s", total);
            cmd.ExecuteNonQuery();

            if (!pendiente)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "INSERT INTO cobros(factura_id,fecha,monto,medio_pago,observaciones) VALUES(@f,@fe,@m,@mp,'Cobro al contado')";
                cmd.Parameters.AddWithValue("@f", facturaId);
                cmd.Parameters.AddWithValue("@fe", fecha);
                cmd.Parameters.AddWithValue("@m", total);
                cmd.Parameters.AddWithValue("@mp", string.IsNullOrEmpty(formaPago) ? "Efectivo" : formaPago);
                cmd.ExecuteNonQuery();
            }
            else if (debitar)
            {
                CcEnTx(conn, tx, clienteId!.Value, "debito", (decimal)total, $"Factura {nro:D8} — Venta #{ventaId}");
            }
            return facturaId;
        }

        /// <summary>Factura las ventas que todavía no tienen factura. Devuelve cuántas creó.</summary>
        public static int GenerarPendientes()
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT id FROM ventas v WHERE NOT EXISTS (SELECT 1 FROM facturas f WHERE f.venta_id=v.id) ORDER BY id";
            var ids = new List<int>();
            using (var r = cmd.ExecuteReader()) while (r.Read()) ids.Add(r.GetInt32(0));
            // Las ventas históricas ya figuran en la cuenta corriente: no se vuelven a debitar.
            foreach (var id in ids) GenerarEnTx(conn, tx, id, debitarCC: false);
            tx.Commit();
            return ids.Count;
        }

        // ── KAWA-46 / KAWA-50 / KAWA-52: consultas ────────────────────────────
        private const string SelectFacturas = @"
            SELECT f.id, f.venta_id, f.cliente_id,
                   COALESCE(TRIM(c.nombre||' '||COALESCE(c.apellido,'')),'Consumidor final'),
                   COALESCE(s.nombre,''), f.nro_factura, f.fecha, f.tipo, f.total,
                   COALESCE((SELECT SUM(monto) FROM cobros WHERE factura_id=f.id),0),
                   f.estado, f.a_cuenta
            FROM facturas f
            LEFT JOIN clientes c ON c.id=f.cliente_id
            LEFT JOIN ventas v ON v.id=f.venta_id
            LEFT JOIN sucursales s ON s.id=v.sucursal_id";

        private static Factura Leer(SqliteDataReader r) => new Factura
        {
            Id = r.GetInt32(0), VentaId = r.GetInt32(1), ClienteId = r.IsDBNull(2) ? null : r.GetInt32(2),
            ClienteNombre = r.GetString(3), SucursalNombre = r.GetString(4), NroFactura = r.GetInt32(5),
            Fecha = r.GetString(6), Tipo = r.GetString(7), Total = (decimal)r.GetDouble(8),
            Cobrado = (decimal)r.GetDouble(9), Estado = r.GetString(10), ACuenta = r.GetInt32(11) == 1
        };

        public static List<Factura> Listar(int? clienteId = null, int? sucursalId = null,
                                           string? desde = null, string? hasta = null, bool soloPendientes = false)
        {
            var list = new List<Factura>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = SelectFacturas + " WHERE 1=1";
            if (clienteId.HasValue) { cmd.CommandText += " AND f.cliente_id=@c"; cmd.Parameters.AddWithValue("@c", clienteId); }
            if (sucursalId.HasValue) { cmd.CommandText += " AND v.sucursal_id=@s"; cmd.Parameters.AddWithValue("@s", sucursalId); }
            if (desde != null) { cmd.CommandText += " AND f.fecha>=@d"; cmd.Parameters.AddWithValue("@d", desde); }
            if (hasta != null) { cmd.CommandText += " AND f.fecha<=@h"; cmd.Parameters.AddWithValue("@h", hasta); }
            if (soloPendientes) cmd.CommandText += " AND f.estado IN ('emitida','parcial')";
            cmd.CommandText += " ORDER BY f.fecha DESC, f.nro_factura DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Leer(r));
            return list;
        }

        public static Factura? Obtener(int facturaId)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = SelectFacturas + " WHERE f.id=@id";
            cmd.Parameters.AddWithValue("@id", facturaId);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Leer(r) : null;
        }

        public static List<FacturaLinea> Detalle(int facturaId)
        {
            var list = new List<FacturaLinea>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT descripcion,cantidad,precio_unitario,subtotal FROM detalle_factura WHERE factura_id=@f ORDER BY id";
            cmd.Parameters.AddWithValue("@f", facturaId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new FacturaLinea { Descripcion = r.GetString(0), Cantidad = r.GetInt32(1),
                    PrecioUnitario = (decimal)r.GetDouble(2), SubTotal = (decimal)r.GetDouble(3) });
            return list;
        }

        public static List<Cobro> Cobros(int facturaId)
        {
            var list = new List<Cobro>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT c.id,c.factura_id,f.nro_factura,c.fecha,c.monto,c.medio_pago,COALESCE(c.observaciones,'')
                                FROM cobros c JOIN facturas f ON f.id=c.factura_id WHERE c.factura_id=@f ORDER BY c.id";
            cmd.Parameters.AddWithValue("@f", facturaId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Cobro { Id = r.GetInt32(0), FacturaId = r.GetInt32(1), NroFactura = r.GetInt32(2), Fecha = r.GetString(3),
                    Monto = (decimal)r.GetDouble(4), MedioPago = r.GetString(5), Observaciones = r.GetString(6) });
            return list;
        }

        // ── KAWA-53: listado mensual ──────────────────────────────────────────
        public static List<Factura> ListadoMensual(int anio, int mes)
        {
            var desde = new DateTime(anio, mes, 1);
            return Listar(desde: desde.ToString("yyyy-MM-dd"), hasta: desde.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd"))
                .Where(f => f.Estado != "anulada").OrderBy(f => f.NroFactura).ToList();
        }

        // ── KAWA-47 / KAWA-48 / KAWA-51: cobros y estado de factura ───────────
        /// <summary>Registra un cobro, actualiza el estado de la factura y acredita la cuenta corriente si corresponde.</summary>
        public static int RegistrarCobro(int facturaId, decimal monto, string medioPago, string observaciones = "")
        {
            if (!MediosDePago.Contains(medioPago)) throw new InvalidOperationException("Medio de pago inválido.");
            monto = Math.Round(monto, 2);
            if (monto <= 0) throw new InvalidOperationException("El monto debe ser mayor a cero.");

            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT f.total, f.estado, f.a_cuenta, f.cliente_id, f.nro_factura,
                                       COALESCE((SELECT SUM(monto) FROM cobros WHERE factura_id=f.id),0)
                                FROM facturas f WHERE f.id=@f";
            cmd.Parameters.AddWithValue("@f", facturaId);
            decimal total, cobrado; string estado; bool aCuenta; int? clienteId; int nro;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) throw new InvalidOperationException("La factura no existe.");
                total = (decimal)r.GetDouble(0); estado = r.GetString(1); aCuenta = r.GetInt32(2) == 1;
                clienteId = r.IsDBNull(3) ? null : r.GetInt32(3); nro = r.GetInt32(4); cobrado = (decimal)r.GetDouble(5);
            }
            if (estado == "anulada") throw new InvalidOperationException("La factura está anulada.");
            decimal pendiente = Math.Round(total - cobrado, 2);
            if (monto > pendiente) throw new InvalidOperationException($"El monto supera el saldo pendiente ({pendiente:C2}).");

            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO cobros(factura_id,fecha,monto,medio_pago,observaciones) VALUES(@f,@fe,@m,@mp,@o);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@f", facturaId);
            cmd.Parameters.AddWithValue("@fe", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@m", (double)monto);
            cmd.Parameters.AddWithValue("@mp", medioPago);
            cmd.Parameters.AddWithValue("@o", observaciones);
            int cobroId = (int)(long)cmd.ExecuteScalar()!;

            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE facturas SET estado=@e WHERE id=@f";
            cmd.Parameters.AddWithValue("@e", monto >= pendiente ? "cobrada" : "parcial");
            cmd.Parameters.AddWithValue("@f", facturaId);
            cmd.ExecuteNonQuery();

            if (aCuenta && clienteId != null)
                CcEnTx(conn, tx, clienteId.Value, "credito", monto, $"Cobro factura {nro:D8} — {medioPago}");

            tx.Commit();
            return cobroId;
        }

        /// <summary>
        /// Anula una factura. Las facturas a cuenta corriente con cobros no pueden anularse;
        /// si la factura debitó la cuenta corriente del cliente, el débito se revierte.
        /// </summary>
        public static void Anular(int facturaId)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT f.total, f.estado, f.a_cuenta, f.debito_cc, f.cliente_id, f.nro_factura,
                                       COALESCE((SELECT SUM(monto) FROM cobros WHERE factura_id=f.id),0)
                                FROM facturas f WHERE f.id=@f";
            cmd.Parameters.AddWithValue("@f", facturaId);
            decimal total, cobrado; string estado; bool aCuenta, debito; int? clienteId; int nro;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) throw new InvalidOperationException("La factura no existe.");
                total = (decimal)r.GetDouble(0); estado = r.GetString(1); aCuenta = r.GetInt32(2) == 1; debito = r.GetInt32(3) == 1;
                clienteId = r.IsDBNull(4) ? null : r.GetInt32(4); nro = r.GetInt32(5); cobrado = (decimal)r.GetDouble(6);
            }
            if (estado == "anulada") throw new InvalidOperationException("La factura ya está anulada.");
            if (aCuenta && cobrado > 0)
                throw new InvalidOperationException("La factura tiene cobros registrados en cuenta corriente y no puede anularse.");

            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE facturas SET estado='anulada' WHERE id=@f";
            cmd.Parameters.AddWithValue("@f", facturaId);
            cmd.ExecuteNonQuery();

            if (debito && clienteId != null)
                CcEnTx(conn, tx, clienteId.Value, "credito", total, $"Anulación factura {nro:D8}");
            tx.Commit();
        }

        /// <summary>Devuelve N° y estado de la factura de una venta, o null si la venta no tiene factura.</summary>
        public static (int Nro, string Estado)? FacturaDeVenta(int ventaId)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT nro_factura, estado FROM facturas WHERE venta_id=@v";
            cmd.Parameters.AddWithValue("@v", ventaId);
            using var r = cmd.ExecuteReader();
            return r.Read() ? (r.GetInt32(0), r.GetString(1)) : null;
        }
    }
}
