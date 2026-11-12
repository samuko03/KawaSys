using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace KawaSysWF.Database
{
    public static class Repository
    {
        // ── USUARIOS ──────────────────────────────────────────────
        public static Usuario? Login(string username, string password)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT u.id, u.nombre, u.apellido, u.email, u.username, u.password_hash,
                       u.rol, u.activo, u.sucursal_id, COALESCE(s.nombre,'')
                FROM usuarios u LEFT JOIN sucursales s ON u.sucursal_id=s.id
                WHERE u.username=@u AND u.activo=1";
            cmd.Parameters.AddWithValue("@u", username);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            var hash = r.GetString(5);
            if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;
            return new Usuario
            {
                Id = r.GetInt32(0), Nombre = r.GetString(1), Apellido = r.GetString(2),
                Email = r.GetString(3), Username = r.GetString(4), PasswordHash = hash,
                Rol = r.GetString(6), Activo = r.GetInt32(7) == 1,
                SucursalId = r.IsDBNull(8) ? null : r.GetInt32(8),
                SucursalNombre = r.GetString(9)
            };
        }

        public static List<Usuario> GetUsuarios()
        {
            var list = new List<Usuario>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT u.id,u.nombre,u.apellido,u.email,u.username,u.rol,u.activo,u.sucursal_id,COALESCE(s.nombre,'')
                FROM usuarios u LEFT JOIN sucursales s ON u.sucursal_id=s.id ORDER BY u.id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Usuario { Id=r.GetInt32(0), Nombre=r.GetString(1), Apellido=r.GetString(2),
                    Email=r.GetString(3), Username=r.GetString(4), Rol=r.GetString(5),
                    Activo=r.GetInt32(6)==1, SucursalId=r.IsDBNull(7)?null:r.GetInt32(7), SucursalNombre=r.GetString(8) });
            return list;
        }

        public static void SaveUsuario(Usuario u, string? newPassword = null)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            if (u.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO usuarios(nombre,apellido,email,username,password_hash,rol,sucursal_id,activo)
                    VALUES(@n,@a,@e,@u,@p,@r,@s,1)";
                cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(newPassword ?? "changeme"));
            }
            else
            {
                if (newPassword != null)
                {
                    cmd.CommandText = @"UPDATE usuarios SET nombre=@n,apellido=@a,email=@e,rol=@r,sucursal_id=@s,activo=@act,password_hash=@p WHERE id=@id";
                    cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(newPassword));
                }
                else
                {
                    cmd.CommandText = @"UPDATE usuarios SET nombre=@n,apellido=@a,email=@e,rol=@r,sucursal_id=@s,activo=@act WHERE id=@id";
                }
                cmd.Parameters.AddWithValue("@act", u.Activo ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", u.Id);
            }
            cmd.Parameters.AddWithValue("@n", u.Nombre);
            cmd.Parameters.AddWithValue("@a", u.Apellido);
            cmd.Parameters.AddWithValue("@e", u.Email);
            cmd.Parameters.AddWithValue("@u", u.Username);
            cmd.Parameters.AddWithValue("@r", u.Rol);
            cmd.Parameters.AddWithValue("@s", (object?)u.SucursalId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // ── SUCURSALES ─────────────────────────────────────────────
        public static List<Sucursal> GetSucursales(bool soloActivas = false)
        {
            var list = new List<Sucursal>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,nombre,direccion,ciudad,telefono,email,activa FROM sucursales" +
                (soloActivas ? " WHERE activa=1" : "") + " ORDER BY nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Sucursal { Id=r.GetInt32(0), Nombre=r.GetString(1),
                    Direccion=r.IsDBNull(2)?"":r.GetString(2), Ciudad=r.IsDBNull(3)?"":r.GetString(3),
                    Telefono=r.IsDBNull(4)?"":r.GetString(4), Email=r.IsDBNull(5)?"":r.GetString(5),
                    Activa=r.GetInt32(6)==1 });
            return list;
        }

        public static void SaveSucursal(Sucursal s)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            if (s.Id == 0)
                cmd.CommandText = "INSERT INTO sucursales(nombre,direccion,ciudad,telefono,email) VALUES(@n,@d,@c,@t,@e)";
            else
            {
                cmd.CommandText = "UPDATE sucursales SET nombre=@n,direccion=@d,ciudad=@c,telefono=@t,email=@e,activa=@a WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", s.Id);
                cmd.Parameters.AddWithValue("@a", s.Activa ? 1 : 0);
            }
            cmd.Parameters.AddWithValue("@n", s.Nombre);
            cmd.Parameters.AddWithValue("@d", s.Direccion);
            cmd.Parameters.AddWithValue("@c", s.Ciudad);
            cmd.Parameters.AddWithValue("@t", s.Telefono);
            cmd.Parameters.AddWithValue("@e", s.Email);
            cmd.ExecuteNonQuery();
        }

        // ── VENTAS ────────────────────────────────────────────────
        public static List<Venta> GetVentas(int? sucursalId = null, string? desde = null, string? hasta = null)
        {
            var list = new List<Venta>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT v.id,v.fecha,v.sucursal_id,COALESCE(s.nombre,''),v.cliente_id,COALESCE(c.nombre||' '||COALESCE(c.apellido,''),''),
                v.marca,v.modelo,v.cantidad,v.precio_unitario,v.total,v.forma_pago,COALESCE(v.observaciones,''),
                (SELECT f.nro_factura FROM facturas f WHERE f.venta_id=v.id)
                FROM ventas v LEFT JOIN sucursales s ON v.sucursal_id=s.id LEFT JOIN clientes c ON v.cliente_id=c.id WHERE 1=1";
            if (sucursalId.HasValue) { cmd.CommandText += " AND v.sucursal_id=@s"; cmd.Parameters.AddWithValue("@s", sucursalId); }
            if (desde != null) { cmd.CommandText += " AND v.fecha>=@d"; cmd.Parameters.AddWithValue("@d", desde); }
            if (hasta != null) { cmd.CommandText += " AND v.fecha<=@h"; cmd.Parameters.AddWithValue("@h", hasta); }
            cmd.CommandText += " ORDER BY v.id DESC LIMIT 200";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Venta { Id=r.GetInt32(0), Fecha=r.GetString(1), SucursalId=r.GetInt32(2),
                    SucursalNombre=r.GetString(3), ClienteId=r.IsDBNull(4)?null:r.GetInt32(4), ClienteNombre=r.GetString(5),
                    Marca=r.IsDBNull(6)?"":r.GetString(6), Modelo=r.GetString(7), Cantidad=r.GetInt32(8),
                    PrecioUnitario=(decimal)r.GetDouble(9), Total=(decimal)r.GetDouble(10),
                    FormaPago=r.IsDBNull(11)?"":r.GetString(11), Observaciones=r.GetString(12),
                    NroFactura=r.IsDBNull(13)?null:r.GetInt32(13) });
            return list;
        }

        /// <summary>Registra la venta y su factura. Devuelve el id de la factura generada.</summary>
        public static int SaveVenta(Venta v)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO ventas(fecha,sucursal_id,cliente_id,marca,modelo,cantidad,precio_unitario,total,forma_pago,observaciones)
                VALUES(@f,@s,@c,@ma,@mo,@ca,@pu,@t,@fp,@ob); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@f", v.Fecha);
            cmd.Parameters.AddWithValue("@s", v.SucursalId);
            cmd.Parameters.AddWithValue("@c", (object?)v.ClienteId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ma", v.Marca);
            cmd.Parameters.AddWithValue("@mo", v.Modelo);
            cmd.Parameters.AddWithValue("@ca", v.Cantidad);
            cmd.Parameters.AddWithValue("@pu", (double)v.PrecioUnitario);
            cmd.Parameters.AddWithValue("@t", (double)v.Total);
            cmd.Parameters.AddWithValue("@fp", v.FormaPago);
            cmd.Parameters.AddWithValue("@ob", v.Observaciones);
            int ventaId = (int)(long)cmd.ExecuteScalar()!;
            // Gestión de motos: la venta descuenta el stock del modelo (y se rechaza si no alcanza).
            MotosRepository.DescontarStockEnTx(conn, tx, v.Marca, v.Modelo, v.Cantidad);
            // KAWA-43: cada venta registrada genera su factura (y su cobro si es al contado).
            int facturaId = FacturacionRepository.GenerarEnTx(conn, tx, ventaId, debitarCC: true);
            tx.Commit();
            return facturaId;
        }

        // Una factura emitida ya fue efectuada: la venta facturada no se modifica ni se elimina.
        private static void ExigirSinFactura(int ventaId, string accion)
        {
            if (FacturacionRepository.FacturaDeVenta(ventaId) is not (int nro, string _)) return;
            throw new InvalidOperationException(
                $"No se puede {accion} la venta #{ventaId}: ya tiene emitida la factura N° {nro:D8}, y una factura emitida cierra la operación.");
        }

        public static void UpdateVenta(Venta v)
        {
            ExigirSinFactura(v.Id, "modificar");
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE ventas SET sucursal_id=@s, cliente_id=@c, marca=@ma, modelo=@mo,
                cantidad=@ca, precio_unitario=@pu, total=@t, forma_pago=@fp, observaciones=@ob WHERE id=@id";
            cmd.Parameters.AddWithValue("@s", v.SucursalId);
            cmd.Parameters.AddWithValue("@c", (object?)v.ClienteId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ma", v.Marca);
            cmd.Parameters.AddWithValue("@mo", v.Modelo);
            cmd.Parameters.AddWithValue("@ca", v.Cantidad);
            cmd.Parameters.AddWithValue("@pu", (double)v.PrecioUnitario);
            cmd.Parameters.AddWithValue("@t", (double)v.Total);
            cmd.Parameters.AddWithValue("@fp", v.FormaPago);
            cmd.Parameters.AddWithValue("@ob", v.Observaciones);
            cmd.Parameters.AddWithValue("@id", v.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteVenta(int id)
        {
            ExigirSinFactura(id, "eliminar");
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ventas WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── FALTANTES ─────────────────────────────────────────────
        public static List<ModeloFaltante> GetFaltantes(int? sucursalId = null)
        {
            var list = new List<ModeloFaltante>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT f.id,f.sucursal_id,COALESCE(s.nombre,''),f.marca,f.modelo,f.cantidad_solicitada,
                COALESCE(f.fecha_generacion,''),COALESCE(f.periodo,''),f.emitido,COALESCE(f.observaciones,'')
                FROM modelos_faltantes f LEFT JOIN sucursales s ON f.sucursal_id=s.id WHERE 1=1";
            if (sucursalId.HasValue) { cmd.CommandText += " AND f.sucursal_id=@s"; cmd.Parameters.AddWithValue("@s", sucursalId); }
            cmd.CommandText += " ORDER BY f.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ModeloFaltante { Id=r.GetInt32(0), SucursalId=r.GetInt32(1), SucursalNombre=r.GetString(2),
                    Marca=r.GetString(3), Modelo=r.GetString(4), CantidadSolicitada=r.GetInt32(5),
                    FechaGeneracion=r.GetString(6), Periodo=r.GetString(7), Emitido=r.GetInt32(8)==1, Observaciones=r.GetString(9) });
            return list;
        }

        public static void SaveFaltante(ModeloFaltante f)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            if (f.Id == 0)
                cmd.CommandText = "INSERT INTO modelos_faltantes(sucursal_id,marca,modelo,cantidad_solicitada,fecha_generacion,periodo,observaciones) VALUES(@s,@ma,@mo,@c,@f,@p,@ob)";
            else
            {
                cmd.CommandText = "UPDATE modelos_faltantes SET sucursal_id=@s,marca=@ma,modelo=@mo,cantidad_solicitada=@c,periodo=@p,observaciones=@ob WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", f.Id);
            }
            cmd.Parameters.AddWithValue("@s", f.SucursalId);
            cmd.Parameters.AddWithValue("@ma", f.Marca);
            cmd.Parameters.AddWithValue("@mo", f.Modelo);
            cmd.Parameters.AddWithValue("@c", f.CantidadSolicitada);
            cmd.Parameters.AddWithValue("@f", f.FechaGeneracion);
            cmd.Parameters.AddWithValue("@p", f.Periodo);
            cmd.Parameters.AddWithValue("@ob", f.Observaciones);
            cmd.ExecuteNonQuery();
        }

        public static void EmitirFaltante(int id)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE modelos_faltantes SET emitido=1 WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteFaltante(int id)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM modelos_faltantes WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── PEDIDOS CLIENTE ───────────────────────────────────────
        public static List<PedidoCliente> GetPedidosCliente()
        {
            var list = new List<PedidoCliente>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT p.id,p.sucursal_id,COALESCE(s.nombre,''),COALESCE(p.cliente_nombre,''),
                COALESCE(p.marca,''),COALESCE(p.modelo,''),p.cantidad,COALESCE(p.fecha,''),COALESCE(p.observaciones,'')
                FROM pedidos_clientes p LEFT JOIN sucursales s ON p.sucursal_id=s.id ORDER BY p.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PedidoCliente { Id=r.GetInt32(0), SucursalId=r.GetInt32(1), SucursalNombre=r.GetString(2),
                    ClienteNombre=r.GetString(3), Marca=r.GetString(4), Modelo=r.GetString(5),
                    Cantidad=r.GetInt32(6), Fecha=r.GetString(7), Observaciones=r.GetString(8) });
            return list;
        }

        public static void SavePedidoCliente(PedidoCliente p)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO pedidos_clientes(sucursal_id,cliente_nombre,marca,modelo,cantidad,fecha,observaciones) VALUES(@s,@cn,@ma,@mo,@c,@f,@ob)";
            cmd.Parameters.AddWithValue("@s", p.SucursalId);
            cmd.Parameters.AddWithValue("@cn", p.ClienteNombre);
            cmd.Parameters.AddWithValue("@ma", p.Marca);
            cmd.Parameters.AddWithValue("@mo", p.Modelo);
            cmd.Parameters.AddWithValue("@c", p.Cantidad);
            cmd.Parameters.AddWithValue("@f", p.Fecha);
            cmd.Parameters.AddWithValue("@ob", p.Observaciones);
            cmd.ExecuteNonQuery();
        }

        public static void UpdatePedidoCliente(PedidoCliente p)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE pedidos_clientes SET sucursal_id=@s, cliente_nombre=@cn, marca=@ma,
                modelo=@mo, cantidad=@c, observaciones=@ob WHERE id=@id";
            cmd.Parameters.AddWithValue("@s", p.SucursalId);
            cmd.Parameters.AddWithValue("@cn", p.ClienteNombre);
            cmd.Parameters.AddWithValue("@ma", p.Marca);
            cmd.Parameters.AddWithValue("@mo", p.Modelo);
            cmd.Parameters.AddWithValue("@c", p.Cantidad);
            cmd.Parameters.AddWithValue("@ob", p.Observaciones);
            cmd.Parameters.AddWithValue("@id", p.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeletePedidoCliente(int id) => DeleteById("pedidos_clientes", id);
        public static void DeleteEnvio(int id)          => DeleteById("envios_listados", id);
        /// <summary>
        /// Elimina el proveedor. Si ya tiene cotizaciones queda inactivo (baja lógica) para conservar el historial.
        /// Devuelve true si se dio de baja lógica en lugar de borrarse.
        /// </summary>
        public static bool DeleteProveedor(int id)
        {
            if (!TieneReferencias(id, ("cotizaciones", "proveedor_id"))) { DeleteById("proveedores", id); return false; }
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE proveedores SET activo=0 WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            return true;
        }

        /// <summary>
        /// Elimina la sucursal. Si ya tiene ventas, usuarios u otros movimientos queda inactiva (baja lógica).
        /// Devuelve true si se dio de baja lógica en lugar de borrarse.
        /// </summary>
        public static bool DeleteSucursal(int id)
        {
            if (!TieneReferencias(id, ("ventas", "sucursal_id"), ("usuarios", "sucursal_id"), ("modelos_faltantes", "sucursal_id"),
                                      ("pedidos_clientes", "sucursal_id"), ("envios_listados", "sucursal_id")))
            { DeleteById("sucursales", id); return false; }
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE sucursales SET activa=0 WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            return true;
        }

        private static bool TieneReferencias(int id, params (string tabla, string columna)[] refs)
        {
            using var conn = DBHelper.GetConnection();
            foreach (var (tabla, columna) in refs)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {tabla} WHERE {columna}=@id";
                cmd.Parameters.AddWithValue("@id", id);
                if ((long)cmd.ExecuteScalar()! > 0) return true;
            }
            return false;
        }
        public static void DeleteUsuario(int id)        => DeleteById("usuarios", id);

        public static void DeleteCliente(int id)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            // Un cliente con ventas o facturas no se elimina: son el respaldo de operaciones ya efectuadas.
            cmd.CommandText = @"SELECT (SELECT COUNT(*) FROM ventas WHERE cliente_id=@id),
                                       (SELECT COUNT(*) FROM facturas WHERE cliente_id=@id)";
            cmd.Parameters.AddWithValue("@id", id);
            using (var r = cmd.ExecuteReader())
            {
                r.Read();
                long ventas = r.GetInt64(0), facturas = r.GetInt64(1);
                if (ventas > 0 || facturas > 0)
                    throw new InvalidOperationException(
                        $"No se puede eliminar el cliente: tiene {ventas} venta(s) y {facturas} factura(s) registradas. " +
                        "Los clientes con operaciones efectuadas no se eliminan.");
            }
            cmd.Parameters.Clear();
            cmd.CommandText = "DELETE FROM transacciones_cc WHERE cuenta_id IN (SELECT id FROM cuentas_corrientes WHERE cliente_id=@id)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM cuentas_corrientes WHERE cliente_id=@id";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM clientes WHERE id=@id";
            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        private static void DeleteById(string table, int id)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {table} WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── ENVÍOS ────────────────────────────────────────────────
        public static List<EnvioListado> GetEnvios()
        {
            var list = new List<EnvioListado>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT e.id,e.sucursal_id,COALESCE(s.nombre,''),e.fecha_envio,COALESCE(e.periodo,''),
                e.cantidad_items,COALESCE(e.observaciones,'')
                FROM envios_listados e LEFT JOIN sucursales s ON e.sucursal_id=s.id ORDER BY e.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new EnvioListado { Id=r.GetInt32(0), SucursalId=r.GetInt32(1), SucursalNombre=r.GetString(2),
                    FechaEnvio=r.GetString(3), Periodo=r.GetString(4), CantidadItems=r.GetInt32(5), Observaciones=r.GetString(6) });
            return list;
        }

        public static void SaveEnvio(EnvioListado e)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO envios_listados(sucursal_id,fecha_envio,periodo,cantidad_items,observaciones) VALUES(@s,@f,@p,@c,@ob)";
            cmd.Parameters.AddWithValue("@s", e.SucursalId);
            cmd.Parameters.AddWithValue("@f", e.FechaEnvio);
            cmd.Parameters.AddWithValue("@p", e.Periodo);
            cmd.Parameters.AddWithValue("@c", e.CantidadItems);
            cmd.Parameters.AddWithValue("@ob", e.Observaciones);
            cmd.ExecuteNonQuery();
        }

        // ── PROVEEDORES ───────────────────────────────────────────
        public static List<Proveedor> GetProveedores(string? tipo = null, string? marca = null, string? modelo = null)
        {
            var list = new List<Proveedor>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,razon_social,COALESCE(cuit,''),tipo,COALESCE(telefono,''),COALESCE(email,''),COALESCE(direccion,''),COALESCE(marcas,''),COALESCE(modelos,'') FROM proveedores WHERE activo=1";
            if (tipo != null) { cmd.CommandText += " AND tipo=@t"; cmd.Parameters.AddWithValue("@t", tipo); }
            if (marca != null) { cmd.CommandText += " AND marcas LIKE @m"; cmd.Parameters.AddWithValue("@m", $"%{marca}%"); }
            if (modelo != null) { cmd.CommandText += " AND modelos LIKE @mo"; cmd.Parameters.AddWithValue("@mo", $"%{modelo}%"); }
            cmd.CommandText += " ORDER BY razon_social";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Proveedor { Id=r.GetInt32(0), RazonSocial=r.GetString(1), Cuit=r.GetString(2),
                    Tipo=r.GetString(3), Telefono=r.GetString(4), Email=r.GetString(5),
                    Direccion=r.GetString(6), Marcas=r.GetString(7), Modelos=r.GetString(8) });
            return list;
        }

        public static void SaveProveedor(Proveedor p)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            if (p.Id == 0)
                cmd.CommandText = "INSERT INTO proveedores(razon_social,cuit,tipo,telefono,email,direccion,marcas,modelos) VALUES(@rs,@c,@t,@te,@e,@d,@ma,@mo)";
            else
            {
                cmd.CommandText = "UPDATE proveedores SET razon_social=@rs,cuit=@c,tipo=@t,telefono=@te,email=@e,direccion=@d,marcas=@ma,modelos=@mo WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", p.Id);
            }
            cmd.Parameters.AddWithValue("@rs", p.RazonSocial);
            cmd.Parameters.AddWithValue("@c", p.Cuit);
            cmd.Parameters.AddWithValue("@t", p.Tipo);
            cmd.Parameters.AddWithValue("@te", p.Telefono);
            cmd.Parameters.AddWithValue("@e", p.Email);
            cmd.Parameters.AddWithValue("@d", p.Direccion);
            cmd.Parameters.AddWithValue("@ma", p.Marcas);
            cmd.Parameters.AddWithValue("@mo", p.Modelos);
            cmd.ExecuteNonQuery();
        }

        // ── SOLICITUDES COMPRA ────────────────────────────────────
        public static List<SolicitudCompra> GetSolicitudesCompra()
        {
            var list = new List<SolicitudCompra>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT s.id,s.fecha,COALESCE(s.descripcion,''),COALESCE(s.estado,'pendiente'),
                    COALESCE((SELECT group_concat(TRIM(COALESCE(i.marca,'')||' '||COALESCE(i.modelo,''))||' x'||i.cantidad, ', ')
                              FROM items_solicitud_compra i WHERE i.solicitud_id=s.id),'')
                FROM solicitudes_compra s ORDER BY s.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SolicitudCompra { Id=r.GetInt32(0), Fecha=r.GetString(1), Descripcion=r.GetString(2), Estado=r.GetString(3), Items=r.GetString(4) });
            return list;
        }

        /// <summary>Registra la solicitud de compra junto con sus ítems (marca, modelo, cantidad) en una sola transacción.</summary>
        public static int SaveSolicitudCompra(SolicitudCompra s, IEnumerable<ItemSolicitud>? items = null)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO solicitudes_compra(fecha,descripcion) VALUES(@f,@d); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@f", s.Fecha);
            cmd.Parameters.AddWithValue("@d", s.Descripcion);
            int id = (int)(long)cmd.ExecuteScalar()!;

            if (items != null)
                foreach (var it in items)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO items_solicitud_compra(solicitud_id,marca,modelo,cantidad) VALUES(@s,@ma,@mo,@c)";
                    cmd.Parameters.AddWithValue("@s", id);
                    cmd.Parameters.AddWithValue("@ma", it.Marca);
                    cmd.Parameters.AddWithValue("@mo", it.Modelo);
                    cmd.Parameters.AddWithValue("@c", it.Cantidad);
                    cmd.ExecuteNonQuery();
                }
            tx.Commit();
            return id;
        }

        // ── COTIZACIONES ──────────────────────────────────────────
        public static List<SolicitudCotizacion> GetSolicitudesCotizacion()
        {
            var list = new List<SolicitudCotizacion>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,solicitud_compra_id,fecha,COALESCE(descripcion,''),COALESCE(estado,'enviada') FROM solicitudes_cotizacion ORDER BY id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SolicitudCotizacion { Id=r.GetInt32(0), SolicitudCompraId=r.GetInt32(1), Fecha=r.GetString(2), Descripcion=r.GetString(3), Estado=r.GetString(4) });
            return list;
        }

        public static void SaveSolicitudCotizacion(SolicitudCotizacion sc)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            if (sc.Id == 0)
                cmd.CommandText = "INSERT INTO solicitudes_cotizacion(solicitud_compra_id,fecha,descripcion) VALUES(@sc,@f,@d)";
            else
            {
                cmd.CommandText = "UPDATE solicitudes_cotizacion SET descripcion=@d,estado=@e WHERE id=@id";
                cmd.Parameters.AddWithValue("@e", sc.Estado);
                cmd.Parameters.AddWithValue("@id", sc.Id);
            }
            cmd.Parameters.AddWithValue("@sc", sc.SolicitudCompraId);
            cmd.Parameters.AddWithValue("@f", sc.Fecha);
            cmd.Parameters.AddWithValue("@d", sc.Descripcion);
            cmd.ExecuteNonQuery();
        }

        public static List<Cotizacion> GetCotizaciones()
        {
            var list = new List<Cotizacion>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT c.id,c.solicitud_cotizacion_id,c.proveedor_id,COALESCE(p.razon_social,''),
                c.fecha,c.monto_total,COALESCE(c.condiciones,''),COALESCE(c.plazo_entrega,''),c.seleccionada
                FROM cotizaciones c LEFT JOIN proveedores p ON c.proveedor_id=p.id ORDER BY c.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Cotizacion { Id=r.GetInt32(0), SolicitudCotizacionId=r.GetInt32(1), ProveedorId=r.GetInt32(2),
                    ProveedorNombre=r.GetString(3), Fecha=r.GetString(4), MontoTotal=(decimal)r.GetDouble(5),
                    Condiciones=r.GetString(6), PlazoEntrega=r.GetString(7), Seleccionada=r.GetInt32(8)==1 });
            return list;
        }

        public static void SaveCotizacion(Cotizacion c)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO cotizaciones(solicitud_cotizacion_id,proveedor_id,fecha,monto_total,condiciones,plazo_entrega) VALUES(@sc,@p,@f,@m,@c,@pl)";
            cmd.Parameters.AddWithValue("@sc", c.SolicitudCotizacionId);
            cmd.Parameters.AddWithValue("@p", c.ProveedorId);
            cmd.Parameters.AddWithValue("@f", c.Fecha);
            cmd.Parameters.AddWithValue("@m", (double)c.MontoTotal);
            cmd.Parameters.AddWithValue("@c", c.Condiciones);
            cmd.Parameters.AddWithValue("@pl", c.PlazoEntrega);
            cmd.ExecuteNonQuery();
        }

        // ── ÓRDENES COMPRA ────────────────────────────────────────
        public static List<OrdenCompra> GetOrdenesCompra()
        {
            var list = new List<OrdenCompra>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT oc.id,oc.cotizacion_id,COALESCE(p.razon_social,''),oc.fecha_emision,
                oc.estado,oc.monto_total,COALESCE(oc.observaciones,'')
                FROM ordenes_compra oc LEFT JOIN cotizaciones c ON oc.cotizacion_id=c.id LEFT JOIN proveedores p ON c.proveedor_id=p.id ORDER BY oc.id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new OrdenCompra { Id=r.GetInt32(0), CotizacionId=r.GetInt32(1), ProveedorNombre=r.GetString(2),
                    FechaEmision=r.GetString(3), Estado=r.GetString(4), MontoTotal=(decimal)r.GetDouble(5), Observaciones=r.GetString(6) });
            return list;
        }

        public static void GenerarOrden(int cotizacionId, decimal monto, string obs)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO ordenes_compra(cotizacion_id,fecha_emision,monto_total,observaciones) VALUES(@c,@f,@m,@ob); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@c", cotizacionId);
            cmd.Parameters.AddWithValue("@f", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@m", (double)monto);
            cmd.Parameters.AddWithValue("@ob", obs);
            long ordenId = (long)cmd.ExecuteScalar()!;
            cmd.CommandText = "UPDATE cotizaciones SET seleccionada=1 WHERE id=@c";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO operaciones(orden_id,descripcion,monto,fecha) VALUES(@oid,@d,@m,@f)";
            cmd.Parameters.AddWithValue("@oid", ordenId);
            cmd.Parameters.AddWithValue("@d", $"Orden de compra #OC-{ordenId:D3}");
            cmd.ExecuteNonQuery();   // la operación pendiente es la que aparece en Finanzas > Aprobaciones
            tx.Commit();
        }

        // ── OPERACIONES FINANCIERAS ───────────────────────────────
        public static List<Operacion> GetOperaciones(string? resultado = null)
        {
            var list = new List<Operacion>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,orden_id,COALESCE(descripcion,''),COALESCE(monto,0),resultado,COALESCE(justificacion,''),fecha,COALESCE(fecha_resolucion,'') FROM operaciones WHERE 1=1";
            if (resultado != null) { cmd.CommandText += " AND resultado=@r"; cmd.Parameters.AddWithValue("@r", resultado); }
            cmd.CommandText += " ORDER BY id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Operacion { Id=r.GetInt32(0), OrdenId=r.IsDBNull(1)?null:r.GetInt32(1),
                    Descripcion=r.GetString(2), Monto=(decimal)r.GetDouble(3), Resultado=r.GetString(4),
                    Justificacion=r.GetString(5), Fecha=r.GetString(6), FechaResolucion=r.GetString(7) });
            return list;
        }

        public static void ResolverOperacion(int id, string resultado, string justificacion)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE operaciones SET resultado=@r,justificacion=@j,fecha_resolucion=@f WHERE id=@id";
            cmd.Parameters.AddWithValue("@r", resultado);
            cmd.Parameters.AddWithValue("@j", justificacion);
            cmd.Parameters.AddWithValue("@f", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            // actualizar estado de orden
            cmd.CommandText = "SELECT orden_id FROM operaciones WHERE id=@id";
            var r = cmd.ExecuteReader();
            if (r.Read() && !r.IsDBNull(0))
            {
                int ordenId = r.GetInt32(0); r.Close();
                cmd.CommandText = "UPDATE ordenes_compra SET estado=@r WHERE id=@oid";
                cmd.Parameters.AddWithValue("@oid", ordenId);
                cmd.ExecuteNonQuery();
            }
        }

        // ── CLIENTES ──────────────────────────────────────────────
        public static List<Cliente> GetClientes()
        {
            var list = new List<Cliente>();
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT c.id,c.nombre,COALESCE(c.apellido,''),COALESCE(c.dni_cuit,''),COALESCE(c.telefono,''),
                COALESCE(c.email,''),COALESCE(c.direccion,''),COALESCE(cc.saldo,0),COALESCE(cc.limite_credito,0)
                FROM clientes c LEFT JOIN cuentas_corrientes cc ON c.id=cc.cliente_id ORDER BY c.nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Cliente { Id=r.GetInt32(0), Nombre=r.GetString(1), Apellido=r.GetString(2),
                    DniCuit=r.GetString(3), Telefono=r.GetString(4), Email=r.GetString(5),
                    Direccion=r.GetString(6), Saldo=(decimal)r.GetDouble(7), LimiteCredito=(decimal)r.GetDouble(8) });
            return list;
        }

        public static void SaveCliente(Cliente c)
        {
            using var conn = DBHelper.GetConnection();
            using var tx = conn.BeginTransaction();
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            if (c.Id == 0)
            {
                cmd.CommandText = "INSERT INTO clientes(nombre,apellido,dni_cuit,telefono,email,direccion,tipo) VALUES(@n,@a,@d,@t,@e,@dir,'mayorista'); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@n", c.Nombre); cmd.Parameters.AddWithValue("@a", c.Apellido);
                cmd.Parameters.AddWithValue("@d", c.DniCuit); cmd.Parameters.AddWithValue("@t", c.Telefono);
                cmd.Parameters.AddWithValue("@e", c.Email); cmd.Parameters.AddWithValue("@dir", c.Direccion);
                long cid = (long)cmd.ExecuteScalar()!;
                cmd.CommandText = "INSERT INTO cuentas_corrientes(cliente_id,saldo,limite_credito) VALUES(@cid,0,@lc)";
                cmd.Parameters.AddWithValue("@cid", cid);
                cmd.Parameters.AddWithValue("@lc", (double)c.LimiteCredito);
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.CommandText = "UPDATE clientes SET nombre=@n,apellido=@a,dni_cuit=@d,telefono=@t,email=@e,direccion=@dir WHERE id=@id";
                cmd.Parameters.AddWithValue("@n", c.Nombre); cmd.Parameters.AddWithValue("@a", c.Apellido);
                cmd.Parameters.AddWithValue("@d", c.DniCuit); cmd.Parameters.AddWithValue("@t", c.Telefono);
                cmd.Parameters.AddWithValue("@e", c.Email); cmd.Parameters.AddWithValue("@dir", c.Direccion);
                cmd.Parameters.AddWithValue("@id", c.Id);
                cmd.ExecuteNonQuery();

                // El límite de crédito también se puede modificar (si el cliente no tenía cuenta corriente, se crea).
                cmd.Parameters.Clear();
                cmd.CommandText = "UPDATE cuentas_corrientes SET limite_credito=@lc WHERE cliente_id=@id";
                cmd.Parameters.AddWithValue("@lc", (double)c.LimiteCredito);
                cmd.Parameters.AddWithValue("@id", c.Id);
                if (cmd.ExecuteNonQuery() == 0)
                {
                    cmd.CommandText = "INSERT INTO cuentas_corrientes(cliente_id,saldo,limite_credito) VALUES(@id,0,@lc)";
                    cmd.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }

        public static void RegistrarTransaccionCC(int clienteId, string tipo, decimal monto, string descripcion)
        {
            using var conn = DBHelper.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id FROM cuentas_corrientes WHERE cliente_id=@c";
            cmd.Parameters.AddWithValue("@c", clienteId);
            var ccId = (long?)cmd.ExecuteScalar();
            if (ccId == null) return;
            cmd.CommandText = tipo == "credito"
                ? "UPDATE cuentas_corrientes SET saldo=saldo+@m WHERE id=@cc"
                : "UPDATE cuentas_corrientes SET saldo=saldo-@m WHERE id=@cc";
            cmd.Parameters.AddWithValue("@m", (double)monto);
            cmd.Parameters.AddWithValue("@cc", ccId);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO transacciones_cc(cuenta_id,fecha,tipo,monto,descripcion) VALUES(@cc,@f,@t,@m,@d)";
            cmd.Parameters.AddWithValue("@f", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@t", tipo);
            cmd.Parameters.AddWithValue("@d", descripcion);
            cmd.ExecuteNonQuery();
        }

        // ── STATS DASHBOARD ───────────────────────────────────────
        public static (int ventas, int faltantes, int ordenesPend, int opPend, int proveedores, int sucursales) GetStats()
        {
            using var conn = DBHelper.GetConnection();
            int Q(string sql) { var c = conn.CreateCommand(); c.CommandText = sql; return (int)(long)c.ExecuteScalar()!; }
            return (
                Q("SELECT COUNT(*) FROM ventas"),
                Q("SELECT COUNT(*) FROM modelos_faltantes WHERE emitido=0"),
                Q("SELECT COUNT(*) FROM ordenes_compra WHERE estado='pendiente'"),
                Q("SELECT COUNT(*) FROM operaciones WHERE resultado='pendiente'"),
                Q("SELECT COUNT(*) FROM proveedores WHERE activo=1"),
                Q("SELECT COUNT(*) FROM sucursales WHERE activa=1")
            );
        }
    }
}
