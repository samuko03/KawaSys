USE KawaSys;
GO

INSERT INTO Sucursal (Nombre, Ciudad) VALUES (N'Casa Central', N'Córdoba'), (N'Sucursal Norte', N'Rosario');

INSERT INTO EstadoVenta (Nombre, Descripcion) VALUES
    (N'Pendiente', N'Venta registrada sin cobrar'),
    (N'Pagada', N'Venta cobrada'),
    (N'Anulada', N'Venta anulada');

INSERT INTO EstadoCompra (Nombre, Descripcion) VALUES
    (N'Solicitada', N'Pedido enviado al proveedor'),
    (N'Recibida', N'Mercadería recibida'),
    (N'Cancelada', N'Compra cancelada');

INSERT INTO EstadoCuentaCorriente (Nombre, Descripcion) VALUES
    (N'Activa', N'Cuenta habilitada'),
    (N'Bloqueada', N'Límite de crédito excedido');

INSERT INTO Moto (Modelo, Marca, Precio, Stock, Cilindrada, Color) VALUES
    (N'CB 190R', N'Honda', 4500000, 10, 190, N'Rojo'),
    (N'XTZ 150', N'Yamaha', 3800000, 6, 150, N'Azul'),
    (N'Gixxer 250', N'Suzuki', 5200000, 4, 250, N'Negro');

INSERT INTO Cliente (Nombre, Telefono, Cuit, Direccion, Email) VALUES
    (N'Juan Pérez', '3511234567', '20-12345678-9', N'Av. Colón 100', N'juan@mail.com');
INSERT INTO CuentaCorriente (IdCliente, IdEstadoCC, Saldo, LimiteCredito) VALUES (1, 1, 0, 2000000);

INSERT INTO Proveedores (Nombre, Tipo, Cuit, Direccion, Telefono, Email) VALUES
    (N'Motos del Sur SA', N'Concesionaria', '30-87654321-0', N'Ruta 9 km 5', '3519876543', N'ventas@motosdelsur.com');
GO
