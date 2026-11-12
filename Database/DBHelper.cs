using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace KawaSysWF.Database
{
    public static class DBHelper
    {
        private static string _dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "kawasys.db");

        public static string ConnectionString => $"Data Source={_dbPath}";

        /// <summary>Apunta a otro archivo de base (lo usan los tests para trabajar sobre una copia).</summary>
        public static void UsarBaseDeDatos(string path) => _dbPath = path;

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static void Initialize()
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS sucursales (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    direccion TEXT,
    ciudad TEXT,
    telefono TEXT,
    email TEXT,
    activa INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS usuarios (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    apellido TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    username TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    rol TEXT NOT NULL,
    activo INTEGER DEFAULT 1,
    sucursal_id INTEGER,
    FOREIGN KEY (sucursal_id) REFERENCES sucursales(id)
);

CREATE TABLE IF NOT EXISTS clientes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    apellido TEXT,
    tipo TEXT DEFAULT 'mayorista',
    dni_cuit TEXT,
    telefono TEXT,
    email TEXT,
    direccion TEXT
);

CREATE TABLE IF NOT EXISTS cuentas_corrientes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id INTEGER UNIQUE,
    saldo REAL DEFAULT 0,
    limite_credito REAL DEFAULT 0,
    FOREIGN KEY (cliente_id) REFERENCES clientes(id)
);

CREATE TABLE IF NOT EXISTS transacciones_cc (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cuenta_id INTEGER,
    fecha TEXT NOT NULL,
    tipo TEXT,
    monto REAL,
    descripcion TEXT,
    FOREIGN KEY (cuenta_id) REFERENCES cuentas_corrientes(id)
);

CREATE TABLE IF NOT EXISTS ventas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha TEXT NOT NULL,
    sucursal_id INTEGER,
    cliente_id INTEGER,
    marca TEXT,
    modelo TEXT NOT NULL,
    cantidad INTEGER DEFAULT 1,
    precio_unitario REAL NOT NULL,
    total REAL NOT NULL,
    forma_pago TEXT,
    observaciones TEXT,
    FOREIGN KEY (sucursal_id) REFERENCES sucursales(id),
    FOREIGN KEY (cliente_id) REFERENCES clientes(id)
);

CREATE TABLE IF NOT EXISTS modelos_faltantes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sucursal_id INTEGER,
    marca TEXT NOT NULL,
    modelo TEXT NOT NULL,
    cantidad_solicitada INTEGER DEFAULT 1,
    fecha_generacion TEXT,
    periodo TEXT,
    emitido INTEGER DEFAULT 0,
    observaciones TEXT,
    FOREIGN KEY (sucursal_id) REFERENCES sucursales(id)
);

CREATE TABLE IF NOT EXISTS pedidos_clientes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sucursal_id INTEGER,
    cliente_nombre TEXT,
    marca TEXT,
    modelo TEXT,
    cantidad INTEGER DEFAULT 1,
    fecha TEXT,
    observaciones TEXT,
    FOREIGN KEY (sucursal_id) REFERENCES sucursales(id)
);

CREATE TABLE IF NOT EXISTS envios_listados (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sucursal_id INTEGER,
    fecha_envio TEXT NOT NULL,
    periodo TEXT,
    cantidad_items INTEGER,
    observaciones TEXT,
    FOREIGN KEY (sucursal_id) REFERENCES sucursales(id)
);

CREATE TABLE IF NOT EXISTS proveedores (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    razon_social TEXT NOT NULL,
    cuit TEXT,
    tipo TEXT DEFAULT 'fijo',
    telefono TEXT,
    email TEXT,
    direccion TEXT,
    marcas TEXT,
    modelos TEXT,
    activo INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS solicitudes_compra (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha TEXT NOT NULL,
    descripcion TEXT,
    estado TEXT DEFAULT 'pendiente',
    observaciones TEXT
);

CREATE TABLE IF NOT EXISTS items_solicitud_compra (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    solicitud_id INTEGER,
    marca TEXT,
    modelo TEXT,
    cantidad INTEGER DEFAULT 1,
    FOREIGN KEY (solicitud_id) REFERENCES solicitudes_compra(id)
);

CREATE TABLE IF NOT EXISTS solicitudes_cotizacion (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    solicitud_compra_id INTEGER,
    fecha TEXT NOT NULL,
    descripcion TEXT,
    estado TEXT DEFAULT 'enviada',
    FOREIGN KEY (solicitud_compra_id) REFERENCES solicitudes_compra(id)
);

CREATE TABLE IF NOT EXISTS cotizaciones (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    solicitud_cotizacion_id INTEGER,
    proveedor_id INTEGER,
    fecha TEXT NOT NULL,
    monto_total REAL,
    condiciones TEXT,
    plazo_entrega TEXT,
    seleccionada INTEGER DEFAULT 0,
    FOREIGN KEY (solicitud_cotizacion_id) REFERENCES solicitudes_cotizacion(id),
    FOREIGN KEY (proveedor_id) REFERENCES proveedores(id)
);

CREATE TABLE IF NOT EXISTS ordenes_compra (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cotizacion_id INTEGER,
    fecha_emision TEXT NOT NULL,
    estado TEXT DEFAULT 'pendiente',
    monto_total REAL,
    observaciones TEXT,
    FOREIGN KEY (cotizacion_id) REFERENCES cotizaciones(id)
);

CREATE TABLE IF NOT EXISTS pedidos_recepcion (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    orden_id INTEGER,
    fecha_despacho TEXT,
    fecha_estimada_llegada TEXT,
    descripcion TEXT,
    estado TEXT DEFAULT 'en_transito',
    FOREIGN KEY (orden_id) REFERENCES ordenes_compra(id)
);

CREATE TABLE IF NOT EXISTS operaciones (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    orden_id INTEGER,
    descripcion TEXT,
    monto REAL,
    resultado TEXT DEFAULT 'pendiente',
    justificacion TEXT,
    fecha TEXT NOT NULL,
    fecha_resolucion TEXT,
    FOREIGN KEY (orden_id) REFERENCES ordenes_compra(id)
);
";
            cmd.ExecuteNonQuery();
            FacturacionRepository.EnsureSchema(conn);
            MotosRepository.EnsureSchema(conn);
            SeedData(conn);
        }

        private static void SeedData(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM usuarios";
            long count = (long)cmd.ExecuteScalar()!;
            if (count > 0) return;

            // ── SUCURSALES ─────────────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO sucursales (nombre, ciudad, direccion, telefono, email) VALUES
('Administración Central','Buenos Aires','Av. Corrientes 1234','011-4444-5555','central@kawasrl.com'),
('Sucursal Norte','Rosario','San Martín 500','0341-123-4567','norte@kawasrl.com'),
('Sucursal Sur','Mar del Plata','Av. Independencia 720','0223-456-7890','sur@kawasrl.com');
";
            cmd.ExecuteNonQuery();

            // ── USUARIOS ───────────────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO usuarios (nombre, apellido, email, username, password_hash, rol, sucursal_id) VALUES
('Admin','Sistema','admin@kawasys.com','admin',@adminHash,'sistema',1),
('Carlos','López','ventas@kawasys.com','ventas',@ventasHash,'ventas',2),
('María','García','compras@kawasys.com','compras',@comprasHash,'compras',1),
('Juan','Pérez','finanzas@kawasys.com','finanzas',@finanzasHash,'finanzas',1),
('Laura','Sánchez','ventas2@kawasys.com','ventas2',@ventasHash,'ventas',3);
";
            cmd.Parameters.AddWithValue("@adminHash",   BCrypt.Net.BCrypt.HashPassword("admin123"));
            cmd.Parameters.AddWithValue("@ventasHash",  BCrypt.Net.BCrypt.HashPassword("ventas123"));
            cmd.Parameters.AddWithValue("@comprasHash", BCrypt.Net.BCrypt.HashPassword("compras123"));
            cmd.Parameters.AddWithValue("@finanzasHash",BCrypt.Net.BCrypt.HashPassword("finanzas123"));
            cmd.ExecuteNonQuery();

            // ── CLIENTES MAYORISTAS ────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO clientes (nombre, apellido, tipo, dni_cuit, telefono, email, direccion) VALUES
('Moto Center','SRL','mayorista','30-71234567-1','011-5555-1111','compras@motocenter.com','Av. Rivadavia 4500, CABA'),
('Distribuidora Del Sur','SA','mayorista','30-68901234-2','0221-333-4444','ventas@delsur.com','Calle 13 Nro 250, La Plata'),
('Motos Patagonia','SRL','mayorista','30-54321098-5','0299-777-8888','info@motospatagonia.com','Av. Argentina 1200, Neuquén'),
('AutoMoto Córdoba','SA','mayorista','30-45678901-3','0351-222-3333','compras@automotocba.com','Bv. San Juan 800, Córdoba');
";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
INSERT INTO cuentas_corrientes (cliente_id, saldo, limite_credito) VALUES
(1, -45000.00, 200000.00),
(2,  12500.00, 150000.00),
(3,   5000.00, 100000.00),
(4, -18750.00, 120000.00);
";
            cmd.ExecuteNonQuery();

            // ── PROVEEDORES ────────────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO proveedores (razon_social, cuit, tipo, telefono, email, direccion, marcas, modelos) VALUES
('Honda Argentina SA',       '30-50001234-1','fijo',      '011-4800-1000','ventas@honda.com.ar',    'Av. del Trabajo 6701, CABA',        'Honda',          'XR150, CB190, Twister, Wave, Biz, CB500'),
('Yamaha Motor Argentina',   '30-51002345-2','fijo',      '011-4802-2000','distribuidores@yamaha.ar','Av. Gral. Paz 14000, GBA',          'Yamaha',         'FZ25, MT-03, YBR125, Crypton, XTZ125'),
('Motomel SA',               '30-52003456-3','fijo',      '011-4700-3000','contacto@motomel.com',   'Florencio Varela 2380, CABA',       'Motomel',        'Skua 150, C110, Cg150, S2, Vitesse 250'),
('Zanella Fábrica',          '30-53004567-4','fijo',      '011-4680-4000','ventas@zanella.com.ar',  'Av. Hipólito Yrigoyen 5400, Lanús', 'Zanella',        'ZB 110, ZTT 200, Rx 150, Due 200, Patagonia'),
('Importadora Kawasaki Sur', '30-61005678-5','alternativo','011-4555-5000','info@kawasur.com',       'Zeballos 3122, Rosario',            'Kawasaki',       'Z400, Ninja 300, KLX140, Versys 300'),
('Distribuidora Beta Motos', '30-62006789-6','alternativo','011-4666-6000','ventas@betamotos.com',   'Av. San Martín 1500, Córdoba',      'Beta,Corven',    'RR 150 Pro, Tempo 150, Mirage 110');
";
            cmd.ExecuteNonQuery();

            // ── VENTAS ─────────────────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO ventas (fecha, sucursal_id, cliente_id, marca, modelo, cantidad, precio_unitario, total, forma_pago, observaciones) VALUES
('2025-05-02', 2, 1, 'Honda',    'XR150',        2, 285000, 570000,  'Transferencia',    'Entrega en sucursal'),
('2025-05-05', 2, NULL,'Yamaha', 'YBR125',        1, 220000, 220000,  'Efectivo',         ''),
('2025-05-08', 3, 2, 'Motomel',  'Skua 150',     3, 195000, 585000,  'Cuenta corriente', 'Descuento 5% por volumen'),
('2025-05-10', 2, NULL,'Zanella','ZB 110',        1, 165000, 165000,  'Crédito',          ''),
('2025-05-12', 3, 3, 'Honda',    'Biz 125',      2, 210000, 420000,  'Transferencia',    ''),
('2025-05-15', 2, 1, 'Yamaha',   'FZ25',         1, 380000, 380000,  'Cuenta corriente', 'Cliente frecuente'),
('2025-05-18', 3, NULL,'Motomel','Cg150',         2, 185000, 370000,  'Efectivo',         ''),
('2025-05-20', 2, 4, 'Honda',    'CB190',         1, 320000, 320000,  'Transferencia',    ''),
('2025-05-22', 3, 2, 'Zanella',  'ZTT 200',      1, 290000, 290000,  'Cheque',           '30 días'),
('2025-05-25', 2, NULL,'Yamaha', 'Crypton 110',   3, 175000, 525000,  'Efectivo',         ''),
('2025-06-01', 2, 1, 'Honda',    'Wave 110',      4, 160000, 640000,  'Cuenta corriente', ''),
('2025-06-02', 3, 4, 'Motomel',  'Vitesse 250',   1, 450000, 450000,  'Transferencia',    ''),
('2025-06-03', 2, NULL,'Honda',  'Twister 250',   1, 520000, 520000,  'Crédito',          ''),
('2025-06-04', 3, 3, 'Yamaha',   'MT-03',         1, 890000, 890000,  'Transferencia',    'Modelo importado'),
('2025-06-05', 2, 2, 'Zanella',  'Due 200',       2, 280000, 560000,  'Cuenta corriente', '');
";
            cmd.ExecuteNonQuery();

            // ── MODELOS FALTANTES ──────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO modelos_faltantes (sucursal_id, marca, modelo, cantidad_solicitada, fecha_generacion, periodo, emitido, observaciones) VALUES
(2, 'Honda',  'XR150',     5, '2025-06-01', '1ra quincena junio 2025', 1, 'Alta rotación, sin stock'),
(2, 'Yamaha', 'FZ25',      3, '2025-06-01', '1ra quincena junio 2025', 1, ''),
(3, 'Motomel','Skua 150',  4, '2025-06-01', '1ra quincena junio 2025', 0, 'Clientes esperando'),
(3, 'Honda',  'Biz 125',   6, '2025-06-01', '1ra quincena junio 2025', 0, ''),
(2, 'Kawasaki','Ninja 300',2, '2025-06-05', '2da quincena junio 2025', 0, 'Pedido especial de cliente'),
(3, 'Zanella','ZTT 200',   3, '2025-06-05', '2da quincena junio 2025', 0, '');
";
            cmd.ExecuteNonQuery();

            // ── PEDIDOS DE CLIENTES ────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO pedidos_clientes (sucursal_id, cliente_nombre, marca, modelo, cantidad, fecha, observaciones) VALUES
(2, 'Roberto Díaz',       'Kawasaki','Ninja 300',  1, '2025-06-03', 'Urgente, lo necesita para el 15'),
(2, 'Fabiana Ruiz',       'Honda',   'XR150',      2, '2025-06-04', 'Color rojo preferencia'),
(3, 'Constructora Pérez', 'Honda',   'Biz 125',    3, '2025-06-04', 'Para uso de empleados'),
(3, 'Agro San Luis SRL',  'Motomel', 'Skua 150',   5, '2025-06-05', 'Uso rural, campo');
";
            cmd.ExecuteNonQuery();

            // ── ENVÍOS A ADMINISTRACIÓN CENTRAL ───────────────────────────────
            cmd.CommandText = @"
INSERT INTO envios_listados (sucursal_id, fecha_envio, periodo, cantidad_items, observaciones) VALUES
(2, '2025-06-02', '1ra quincena junio 2025', 2, 'Enviado por email y sistema'),
(3, '2025-06-02', '1ra quincena junio 2025', 2, 'Enviado por sistema');
";
            cmd.ExecuteNonQuery();

            // ── SOLICITUDES DE COMPRA ──────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO solicitudes_compra (fecha, descripcion, estado) VALUES
('2025-06-03', 'Reposición urgente Honda XR150 y Yamaha FZ25 — Sucursal Norte', 'pendiente'),
('2025-06-03', 'Reposición Motomel Skua 150 y Honda Biz 125 — Sucursal Sur',    'pendiente'),
('2025-05-15', 'Compra mensual Mayo — modelos varios',                            'procesada');

INSERT INTO items_solicitud_compra (solicitud_id, marca, modelo, cantidad) VALUES
(1, 'Honda',   'XR150',    5),
(1, 'Yamaha',  'FZ25',     3),
(2, 'Motomel', 'Skua 150', 4),
(2, 'Honda',   'Biz 125',  6),
(3, 'Honda',   'Wave 110', 8),
(3, 'Zanella', 'ZB 110',   5);
";
            cmd.ExecuteNonQuery();

            // ── SOLICITUDES DE COTIZACIÓN ──────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO solicitudes_cotizacion (solicitud_compra_id, fecha, descripcion, estado) VALUES
(1, '2025-06-03', 'Cotización a Honda Argentina y Yamaha por reposición urgente', 'enviada'),
(2, '2025-06-03', 'Cotización a Motomel por Skua y Honda por Biz',                'respondida'),
(3, '2025-05-15', 'Cotización mayo — Honda y Zanella',                             'respondida');
";
            cmd.ExecuteNonQuery();

            // ── COTIZACIONES RECIBIDAS ─────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO cotizaciones (solicitud_cotizacion_id, proveedor_id, fecha, monto_total, condiciones, plazo_entrega, seleccionada) VALUES
(2, 3, '2025-06-04', 1755000, 'Contado -3%',          '7 días hábiles',  1),
(2, 1, '2025-06-04', 1860000, '30 días sin interés',  '10 días hábiles', 0),
(3, 1, '2025-05-16', 2080000, 'Contado',              '5 días hábiles',  1),
(3, 4, '2025-05-17', 1980000, '15 días',              '8 días hábiles',  0);
";
            cmd.ExecuteNonQuery();

            // ── ÓRDENES DE COMPRA ──────────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO ordenes_compra (cotizacion_id, fecha_emision, estado, monto_total, observaciones) VALUES
(1, '2025-06-04', 'aprobada',  1755000, 'Cotización Motomel seleccionada por mejor precio'),
(3, '2025-05-16', 'recibida',  2080000, 'Orden mayo — recibida el 22/05');
";
            cmd.ExecuteNonQuery();

            // ── PEDIDOS DE RECEPCIÓN ───────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO pedidos_recepcion (orden_id, fecha_despacho, fecha_estimada_llegada, descripcion, estado) VALUES
(2, '2025-05-19', '2025-05-22', '8x Honda Wave 110 + 5x Zanella ZB 110', 'recibido');
";
            cmd.ExecuteNonQuery();

            // ── OPERACIONES FINANCIERAS ────────────────────────────────────────
            cmd.CommandText = @"
INSERT INTO operaciones (orden_id, descripcion, monto, resultado, justificacion, fecha, fecha_resolucion) VALUES
(1, 'Orden de compra #OC-001 — Motomel (Skua 150 + Biz 125)', 1755000, 'aprobada',
 'Precio competitivo, proveedor con entrega rápida', '2025-06-04', '2025-06-04'),
(2, 'Orden de compra #OC-002 — Honda (Wave 110 + ZB 110)',     2080000, 'aprobada',
 'Compra mensual aprobada según presupuesto',         '2025-05-16', '2025-05-16');

INSERT INTO operaciones (descripcion, monto, resultado, fecha) VALUES
('Pago a proveedor Yamaha — Factura 00120045', 950000, 'pendiente', '2025-06-05'),
('Nota de crédito Motomel — devolución',        45000, 'pendiente', '2025-06-06');
";
            cmd.ExecuteNonQuery();

            // ── TRANSACCIONES CUENTA CORRIENTE ────────────────────────────────
            cmd.CommandText = @"
INSERT INTO transacciones_cc (cuenta_id, fecha, tipo, monto, descripcion) VALUES
(1, '2025-05-02', 'debito',  570000, 'Venta #1 — 2x Honda XR150'),
(1, '2025-05-15', 'debito',  380000, 'Venta #6 — 1x Yamaha FZ25'),
(1, '2025-05-30', 'credito', 500000, 'Pago parcial — transferencia'),
(1, '2025-06-01', 'debito',  640000, 'Venta #11 — 4x Honda Wave 110'),
(2, '2025-05-08', 'debito',  585000, 'Venta #3 — 3x Motomel Skua 150'),
(2, '2025-05-22', 'debito',  290000, 'Venta #9 — 1x Zanella ZTT 200'),
(2, '2025-05-28', 'credito', 875000, 'Cobro total — depósito'),
(2, '2025-06-05', 'debito',  560000, 'Venta #15 — 2x Zanella Due 200'),
(3, '2025-05-12', 'debito',  420000, 'Venta #5 — 2x Honda Biz 125'),
(3, '2025-05-25', 'credito', 425000, 'Pago — transferencia bancaria'),
(4, '2025-05-20', 'debito',  320000, 'Venta #8 — 1x Honda CB190'),
(4, '2025-06-02', 'debito',  450000, 'Venta #12 — 1x Motomel Vitesse 250'),
(4, '2025-06-03', 'credito', 200000, 'Pago parcial efectivo');
";
            cmd.ExecuteNonQuery();
        }
    }
}
