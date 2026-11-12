using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace KawaSysWF.Database
{
    /// <summary>
    /// Gestión de modelos de motos (RG): registrar, modificar, dar de baja, consultar y actualizar stock.
    /// Las reglas de negocio se validan acá para que valgan igual desde la pantalla y desde los tests.
    /// </summary>
    public static class MotosRepository
    {
        public static void EnsureSchema(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS motos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    marca TEXT NOT NULL,
    modelo TEXT NOT NULL,
    cilindrada INTEGER DEFAULT 0,
    color TEXT,
    precio REAL NOT NULL,
    stock INTEGER NOT NULL DEFAULT 0,
    activa INTEGER NOT NULL DEFAULT 1
);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT COUNT(*) FROM motos";
            if ((long)cmd.ExecuteScalar()! > 0) return;

            // Catálogo inicial: los modelos que ya se venden en KawaSys.
            cmd.CommandText = @"
INSERT INTO motos (marca, modelo, cilindrada, color, precio, stock) VALUES
('Honda',   'XR150',       150, 'Rojo',   285000, 8),
('Honda',   'Biz 125',     125, 'Blanco', 210000, 6),
('Honda',   'CB190',       190, 'Negro',  320000, 4),
('Honda',   'Wave 110',    110, 'Azul',   160000, 10),
('Honda',   'Twister 250', 250, 'Rojo',   520000, 3),
('Yamaha',  'YBR125',      125, 'Negro',  220000, 5),
('Yamaha',  'FZ25',        250, 'Azul',   380000, 6),
('Yamaha',  'Crypton 110', 110, 'Gris',   175000, 7),
('Yamaha',  'MT-03',       321, 'Negro',  890000, 2),
('Motomel', 'Skua 150',    150, 'Verde',  195000, 9),
('Motomel', 'Cg150',       150, 'Rojo',   185000, 5),
('Motomel', 'Vitesse 250', 250, 'Negro',  450000, 2),
('Zanella', 'ZB 110',      110, 'Rojo',   165000, 6),
('Zanella', 'ZTT 200',     200, 'Negro',  290000, 3),
('Zanella', 'Due 200',     200, 'Blanco', 280000, 4),
('Kawasaki','Ninja 300',   300, 'Verde',  950000, 0);";
            cmd.ExecuteNonQuery();
        }

        private static Moto Leer(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0), Marca = r.GetString(1), Modelo = r.GetString(2),
            Cilindrada = r.IsDBNull(3) ? 0 : r.GetInt32(3), Color = r.IsDBNull(4) ? "" : r.GetString(4),
            Precio = (decimal)r.GetDouble(5), Stock = r.GetInt32(6), Activa = r.GetInt32(7) == 1
        };

        private const string Columnas = "id, marca, modelo, cilindrada, color, precio, stock, activa";

        /// <summary>Consulta por marca o modelo (texto parcial, sin distinguir mayúsculas).</summary>
        public static List<Moto> Buscar(string? texto = null, bool soloActivas = false)
        {
            var lista = new List<Moto>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {Columnas} FROM motos WHERE 1=1";
            if (soloActivas) cmd.CommandText += " AND activa=1";
            if (!string.IsNullOrWhiteSpace(texto))
            {
                cmd.CommandText += " AND (marca LIKE @t OR modelo LIKE @t OR (marca || ' ' || modelo) LIKE @t)";
                cmd.Parameters.AddWithValue("@t", $"%{texto.Trim()}%");
            }
            cmd.CommandText += " ORDER BY marca, modelo";
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(Leer(r));
            return lista;
        }

        public static Moto? Obtener(int id)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {Columnas} FROM motos WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Leer(r) : null;
        }

        private static void Validar(Moto m)
        {
            if (string.IsNullOrWhiteSpace(m.Marca) || string.IsNullOrWhiteSpace(m.Modelo))
                throw new InvalidOperationException("La marca y el modelo son obligatorios.");
            if (m.Precio <= 0) throw new InvalidOperationException("El precio debe ser mayor a cero.");
            if (m.Stock < 0) throw new InvalidOperationException("El stock no puede ser negativo.");
            if (m.Cilindrada < 0) throw new InvalidOperationException("La cilindrada no puede ser negativa.");
            if (!m.Activa) return;

            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(*) FROM motos WHERE activa=1 AND id<>@id
                AND lower(trim(marca))=lower(@ma) AND lower(trim(modelo))=lower(@mo)";
            cmd.Parameters.AddWithValue("@id", m.Id);
            cmd.Parameters.AddWithValue("@ma", m.Marca.Trim());
            cmd.Parameters.AddWithValue("@mo", m.Modelo.Trim());
            if ((long)cmd.ExecuteScalar()! > 0)
                throw new InvalidOperationException($"Ya existe un modelo activo {m.Marca.Trim()} {m.Modelo.Trim()} en el catálogo.");
        }

        /// <summary>RD: registrar un modelo nuevo. Devuelve su id.</summary>
        public static int Registrar(Moto m)
        {
            m.Activa = true;
            Validar(m);
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO motos (marca, modelo, cilindrada, color, precio, stock, activa)
                VALUES (@ma, @mo, @ci, @co, @p, @s, 1); SELECT last_insert_rowid();";
            Parametros(cmd, m);
            return (int)(long)cmd.ExecuteScalar()!;
        }

        /// <summary>RD: modificar los datos de un modelo registrado (también permite reactivarlo).</summary>
        public static void Modificar(Moto m)
        {
            if (Obtener(m.Id) == null) throw new InvalidOperationException($"No existe el modelo #{m.Id}.");
            Validar(m);
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE motos SET marca=@ma, modelo=@mo, cilindrada=@ci, color=@co, precio=@p, stock=@s, activa=@a
                WHERE id=@id";
            Parametros(cmd, m);
            cmd.Parameters.AddWithValue("@a", m.Activa ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", m.Id);
            cmd.ExecuteNonQuery();
        }

        private static void Parametros(SqliteCommand cmd, Moto m)
        {
            cmd.Parameters.AddWithValue("@ma", m.Marca.Trim());
            cmd.Parameters.AddWithValue("@mo", m.Modelo.Trim());
            cmd.Parameters.AddWithValue("@ci", m.Cilindrada);
            cmd.Parameters.AddWithValue("@co", m.Color.Trim());
            cmd.Parameters.AddWithValue("@p", (double)m.Precio);
            cmd.Parameters.AddWithValue("@s", m.Stock);
        }

        /// <summary>RD: dar de baja un modelo. Es baja lógica: se conserva para el historial de ventas.</summary>
        public static void DarDeBaja(int id)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE motos SET activa=0 WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            if (cmd.ExecuteNonQuery() == 0) throw new InvalidOperationException($"No existe el modelo #{id}.");
        }

        /// <summary>RD: actualizar el stock sumando (ingreso) o restando (ajuste) unidades. Devuelve el stock nuevo.</summary>
        public static int ActualizarStock(int id, int unidades)
        {
            var m = Obtener(id) ?? throw new InvalidOperationException($"No existe el modelo #{id}.");
            if (unidades == 0) throw new InvalidOperationException("Indicá cuántas unidades sumar o restar.");
            int nuevo = m.Stock + unidades;
            if (nuevo < 0)
                throw new InvalidOperationException($"El stock no puede quedar negativo: hay {m.Stock} unidades de {m.Descripcion} y se quieren restar {-unidades}.");
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE motos SET stock=@s WHERE id=@id";
            cmd.Parameters.AddWithValue("@s", nuevo);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            return nuevo;
        }

        /// <summary>
        /// Al registrar una venta descuenta el stock del modelo, dentro de la misma transacción.
        /// Si el modelo está en el catálogo pero dado de baja o sin stock suficiente, la venta se rechaza.
        /// Los modelos que no están en el catálogo (ventas cargadas antes de existir) no tocan stock.
        /// </summary>
        internal static void DescontarStockEnTx(SqliteConnection conn, SqliteTransaction tx, string marca, string modelo, int cantidad)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT id, stock, activa FROM motos
                WHERE lower(trim(marca))=lower(@ma) AND lower(trim(modelo))=lower(@mo)
                ORDER BY activa DESC, id LIMIT 1";
            cmd.Parameters.AddWithValue("@ma", marca.Trim());
            cmd.Parameters.AddWithValue("@mo", modelo.Trim());
            int id, stock; bool activa;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return;
                id = r.GetInt32(0); stock = r.GetInt32(1); activa = r.GetInt32(2) == 1;
            }
            if (!activa)
                throw new InvalidOperationException($"El modelo {marca} {modelo} está dado de baja y no se puede vender.");
            if (cantidad > stock)
                throw new InvalidOperationException($"Stock insuficiente de {marca} {modelo}: hay {stock} y se quieren vender {cantidad}.");

            var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE motos SET stock = stock - @c WHERE id=@id";
            upd.Parameters.AddWithValue("@c", cantidad);
            upd.Parameters.AddWithValue("@id", id);
            upd.ExecuteNonQuery();
        }
    }
}
