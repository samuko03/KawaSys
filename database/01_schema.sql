IF DB_ID('KawaSys') IS NULL CREATE DATABASE KawaSys;
GO
USE KawaSys;
GO

CREATE TABLE Sucursal (
    IdSucursal INT IDENTITY PRIMARY KEY,
    Nombre     NVARCHAR(100) NOT NULL,
    Ciudad     NVARCHAR(100) NOT NULL
);

CREATE TABLE Moto (
    IdMoto     INT IDENTITY PRIMARY KEY,
    Modelo     NVARCHAR(100) NOT NULL,
    Marca      NVARCHAR(100) NOT NULL,
    Precio     DECIMAL(18,2) NOT NULL CHECK (Precio >= 0),
    Stock      INT NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    Cilindrada INT NOT NULL CHECK (Cilindrada > 0),
    Color      NVARCHAR(50) NOT NULL,
    Activo     BIT NOT NULL DEFAULT 1
);

CREATE TABLE EstadoCuentaCorriente (
    IdEstadoCC  INT IDENTITY PRIMARY KEY,
    Nombre      NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(200) NULL
);

CREATE TABLE Cliente (
    IdCliente INT IDENTITY PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Telefono  VARCHAR(30) NULL,
    Cuit      VARCHAR(13) NULL,
    Direccion NVARCHAR(200) NULL,
    Email     NVARCHAR(150) NULL,
    Activo    BIT NOT NULL DEFAULT 1
);

CREATE TABLE CuentaCorriente (
    IdCuenta      INT IDENTITY PRIMARY KEY,
    IdCliente     INT NOT NULL UNIQUE REFERENCES Cliente(IdCliente),
    IdEstadoCC    INT NOT NULL REFERENCES EstadoCuentaCorriente(IdEstadoCC),
    Saldo         DECIMAL(18,2) NOT NULL DEFAULT 0,
    LimiteCredito DECIMAL(18,2) NOT NULL DEFAULT 0
);

CREATE TABLE EstadoVenta (
    IdEstadoVenta INT IDENTITY PRIMARY KEY,
    Nombre        NVARCHAR(50) NOT NULL,
    Descripcion   NVARCHAR(200) NULL
);

CREATE TABLE Ventas (
    IdVenta       INT IDENTITY PRIMARY KEY,
    IdCliente     INT NOT NULL REFERENCES Cliente(IdCliente),
    IdSucursal    INT NOT NULL REFERENCES Sucursal(IdSucursal),
    IdEstadoVenta INT NOT NULL REFERENCES EstadoVenta(IdEstadoVenta),
    Fecha         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Total         DECIMAL(18,2) NOT NULL DEFAULT 0
);

CREATE TABLE DetalleVenta (
    IdDetalleVenta INT IDENTITY PRIMARY KEY,
    IdVenta        INT NOT NULL REFERENCES Ventas(IdVenta) ON DELETE CASCADE,
    IdMoto         INT NOT NULL REFERENCES Moto(IdMoto),
    Cantidad       INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    SubTotal       AS (Cantidad * PrecioUnitario) PERSISTED
);

CREATE TABLE Factura (
    IdFactura  INT IDENTITY PRIMARY KEY,
    IdVenta    INT NOT NULL UNIQUE REFERENCES Ventas(IdVenta),
    IdCliente  INT NOT NULL REFERENCES Cliente(IdCliente),
    NroFactura INT NOT NULL UNIQUE,
    Fecha      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Tipo       CHAR(1) NOT NULL CHECK (Tipo IN ('A','B','C')),
    Total      DECIMAL(18,2) NOT NULL,
    Estado     NVARCHAR(20) NOT NULL DEFAULT 'Emitida'
);

CREATE TABLE DetalleFactura (
    IdDetalleFactura INT IDENTITY PRIMARY KEY,
    IdFactura        INT NOT NULL REFERENCES Factura(IdFactura) ON DELETE CASCADE,
    Cantidad         INT NOT NULL,
    PrecioUnitario   DECIMAL(18,2) NOT NULL,
    SubTotal         AS (Cantidad * PrecioUnitario) PERSISTED
);

CREATE TABLE Cobro (
    IdCobro   INT IDENTITY PRIMARY KEY,
    IdFactura INT NOT NULL REFERENCES Factura(IdFactura),
    Fecha     DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Monto     DECIMAL(18,2) NOT NULL CHECK (Monto > 0),
    MedioPago NVARCHAR(30) NOT NULL
);

CREATE TABLE Proveedores (
    IdProveedor INT IDENTITY PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL,
    Tipo        NVARCHAR(50) NULL,
    Cuit        VARCHAR(13) NULL,
    Direccion   NVARCHAR(200) NULL,
    Telefono    VARCHAR(30) NULL,
    Email       NVARCHAR(150) NULL,
    Activo      BIT NOT NULL DEFAULT 1
);

CREATE TABLE EstadoCompra (
    IdEstadoCompra INT IDENTITY PRIMARY KEY,
    Nombre         NVARCHAR(50) NOT NULL,
    Descripcion    NVARCHAR(200) NULL
);

CREATE TABLE Cotizacion (
    IdCotizacion  INT IDENTITY PRIMARY KEY,
    IdProveedor   INT NOT NULL REFERENCES Proveedores(IdProveedor),
    Fecha         DATE NOT NULL,
    PrecioTotal   DECIMAL(18,2) NOT NULL,
    PlazoEntrega  INT NOT NULL,
    CondicionPago NVARCHAR(100) NULL,
    Validez       DATE NOT NULL
);

CREATE TABLE Compras (
    IdCompra       INT IDENTITY PRIMARY KEY,
    IdProveedor    INT NOT NULL REFERENCES Proveedores(IdProveedor),
    IdSucursal     INT NOT NULL REFERENCES Sucursal(IdSucursal),
    IdEstadoCompra INT NOT NULL REFERENCES EstadoCompra(IdEstadoCompra),
    IdCotizacion   INT NULL REFERENCES Cotizacion(IdCotizacion),
    Fecha          DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE DetalleCompra (
    IdDetalleCompra INT IDENTITY PRIMARY KEY,
    IdCompra        INT NOT NULL REFERENCES Compras(IdCompra) ON DELETE CASCADE,
    IdMoto          INT NOT NULL REFERENCES Moto(IdMoto),
    Cantidad        INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(18,2) NOT NULL,
    SubTotal        AS (Cantidad * PrecioUnitario) PERSISTED
);

CREATE TABLE PedidoCliente (
    IdPedido        INT IDENTITY PRIMARY KEY,
    IdCliente       INT NULL REFERENCES Cliente(IdCliente),
    ModeloSolicitado NVARCHAR(100) NOT NULL,
    Fecha           DATE NOT NULL DEFAULT CAST(SYSDATETIME() AS DATE),
    Cancelado       BIT NOT NULL DEFAULT 0
);

CREATE TABLE ModeloFaltante (
    IdFaltante INT IDENTITY PRIMARY KEY,
    IdSucursal INT NOT NULL REFERENCES Sucursal(IdSucursal),
    Modelo     NVARCHAR(100) NOT NULL,
    Fecha      DATE NOT NULL DEFAULT CAST(SYSDATETIME() AS DATE)
);
GO
