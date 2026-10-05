from sqlalchemy import Column, Integer, String, Float, Date, DateTime, ForeignKey, Enum, Text, Boolean
from sqlalchemy.orm import relationship
from database import Base
from datetime import datetime
import enum


class RolUsuario(str, enum.Enum):
    ventas = "ventas"
    compras = "compras"
    finanzas = "finanzas"
    sistema = "sistema"


class TipoProveedor(str, enum.Enum):
    fijo = "fijo"
    alternativo = "alternativo"


class EstadoOrden(str, enum.Enum):
    pendiente = "pendiente"
    aprobada = "aprobada"
    emitida = "emitida"
    recibida = "recibida"
    rechazada = "rechazada"


class ResultadoOperacion(str, enum.Enum):
    aprobada = "aprobada"
    rechazada = "rechazada"
    pendiente = "pendiente"


class Usuario(Base):
    __tablename__ = "usuarios"
    id = Column(Integer, primary_key=True)
    nombre = Column(String(100), nullable=False)
    apellido = Column(String(100), nullable=False)
    email = Column(String(150), unique=True, nullable=False)
    username = Column(String(50), unique=True, nullable=False)
    password_hash = Column(String(256), nullable=False)
    rol = Column(String(20), nullable=False)
    activo = Column(Boolean, default=True)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"), nullable=True)
    sucursal = relationship("Sucursal", back_populates="usuarios")
    created_at = Column(DateTime, default=datetime.utcnow)


class Sucursal(Base):
    __tablename__ = "sucursales"
    id = Column(Integer, primary_key=True)
    nombre = Column(String(150), nullable=False)
    direccion = Column(String(250))
    ciudad = Column(String(100))
    telefono = Column(String(50))
    email = Column(String(150))
    activa = Column(Boolean, default=True)
    usuarios = relationship("Usuario", back_populates="sucursal")
    ventas = relationship("Venta", back_populates="sucursal")
    envios = relationship("EnvioListado", back_populates="sucursal")


class Moto(Base):
    __tablename__ = "motos"
    id = Column(Integer, primary_key=True)
    marca = Column(String(100), nullable=False)
    modelo = Column(String(100), nullable=False)
    año = Column(Integer)
    precio_base = Column(Float)
    stock = Column(Integer, default=0)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"))
    sucursal = relationship("Sucursal")


class Cliente(Base):
    __tablename__ = "clientes"
    id = Column(Integer, primary_key=True)
    nombre = Column(String(150), nullable=False)
    apellido = Column(String(150))
    tipo = Column(String(20), default="mayorista")
    dni_cuit = Column(String(30))
    telefono = Column(String(50))
    email = Column(String(150))
    direccion = Column(String(250))
    ventas = relationship("Venta", back_populates="cliente")
    cuenta_corriente = relationship("CuentaCorriente", back_populates="cliente", uselist=False)


class Venta(Base):
    __tablename__ = "ventas"
    id = Column(Integer, primary_key=True)
    fecha = Column(Date, nullable=False)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"), nullable=False)
    cliente_id = Column(Integer, ForeignKey("clientes.id"), nullable=True)
    modelo = Column(String(150), nullable=False)
    marca = Column(String(100))
    cantidad = Column(Integer, default=1)
    precio_unitario = Column(Float, nullable=False)
    total = Column(Float, nullable=False)
    forma_pago = Column(String(50))
    observaciones = Column(Text)
    sucursal = relationship("Sucursal", back_populates="ventas")
    cliente = relationship("Cliente", back_populates="ventas")


class ModeloFaltante(Base):
    __tablename__ = "modelos_faltantes"
    id = Column(Integer, primary_key=True)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"), nullable=False)
    marca = Column(String(100), nullable=False)
    modelo = Column(String(100), nullable=False)
    cantidad_solicitada = Column(Integer, default=1)
    fecha_generacion = Column(Date)
    periodo = Column(String(20))
    emitido = Column(Boolean, default=False)
    observaciones = Column(Text)
    sucursal = relationship("Sucursal")


class PedidoCliente(Base):
    __tablename__ = "pedidos_clientes"
    id = Column(Integer, primary_key=True)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"))
    cliente_nombre = Column(String(200))
    marca = Column(String(100))
    modelo = Column(String(100))
    cantidad = Column(Integer, default=1)
    fecha = Column(Date)
    observaciones = Column(Text)
    sucursal = relationship("Sucursal")


class EnvioListado(Base):
    __tablename__ = "envios_listados"
    id = Column(Integer, primary_key=True)
    sucursal_id = Column(Integer, ForeignKey("sucursales.id"), nullable=False)
    fecha_envio = Column(Date, nullable=False)
    periodo = Column(String(20))
    cantidad_items = Column(Integer)
    observaciones = Column(Text)
    sucursal = relationship("Sucursal", back_populates="envios")


class Proveedor(Base):
    __tablename__ = "proveedores"
    id = Column(Integer, primary_key=True)
    razon_social = Column(String(200), nullable=False)
    cuit = Column(String(30))
    tipo = Column(String(20), default="fijo")
    telefono = Column(String(50))
    email = Column(String(150))
    direccion = Column(String(250))
    marcas = Column(Text)
    modelos = Column(Text)
    activo = Column(Boolean, default=True)
    cotizaciones = relationship("Cotizacion", back_populates="proveedor")


class SolicitudCompra(Base):
    __tablename__ = "solicitudes_compra"
    id = Column(Integer, primary_key=True)
    fecha = Column(Date, nullable=False)
    descripcion = Column(Text)
    estado = Column(String(30), default="pendiente")
    observaciones = Column(Text)
    items = relationship("ItemSolicitudCompra", back_populates="solicitud")
    solicitudes_cotizacion = relationship("SolicitudCotizacion", back_populates="solicitud_compra")


class ItemSolicitudCompra(Base):
    __tablename__ = "items_solicitud_compra"
    id = Column(Integer, primary_key=True)
    solicitud_id = Column(Integer, ForeignKey("solicitudes_compra.id"))
    marca = Column(String(100))
    modelo = Column(String(100))
    cantidad = Column(Integer, default=1)
    solicitud = relationship("SolicitudCompra", back_populates="items")


class SolicitudCotizacion(Base):
    __tablename__ = "solicitudes_cotizacion"
    id = Column(Integer, primary_key=True)
    solicitud_compra_id = Column(Integer, ForeignKey("solicitudes_compra.id"))
    fecha = Column(Date, nullable=False)
    descripcion = Column(Text)
    estado = Column(String(30), default="enviada")
    solicitud_compra = relationship("SolicitudCompra", back_populates="solicitudes_cotizacion")
    cotizaciones = relationship("Cotizacion", back_populates="solicitud_cotizacion")


class Cotizacion(Base):
    __tablename__ = "cotizaciones"
    id = Column(Integer, primary_key=True)
    solicitud_cotizacion_id = Column(Integer, ForeignKey("solicitudes_cotizacion.id"))
    proveedor_id = Column(Integer, ForeignKey("proveedores.id"))
    fecha = Column(Date, nullable=False)
    monto_total = Column(Float)
    condiciones = Column(Text)
    plazo_entrega = Column(String(100))
    seleccionada = Column(Boolean, default=False)
    proveedor = relationship("Proveedor", back_populates="cotizaciones")
    solicitud_cotizacion = relationship("SolicitudCotizacion", back_populates="cotizaciones")
    ordenes = relationship("OrdenCompra", back_populates="cotizacion")


class OrdenCompra(Base):
    __tablename__ = "ordenes_compra"
    id = Column(Integer, primary_key=True)
    cotizacion_id = Column(Integer, ForeignKey("cotizaciones.id"))
    fecha_emision = Column(Date, nullable=False)
    estado = Column(String(20), default="pendiente")
    monto_total = Column(Float)
    observaciones = Column(Text)
    cotizacion = relationship("Cotizacion", back_populates="ordenes")
    operacion = relationship("Operacion", back_populates="orden", uselist=False)
    pedidos_recepcion = relationship("PedidoRecepcion", back_populates="orden")


class PedidoRecepcion(Base):
    __tablename__ = "pedidos_recepcion"
    id = Column(Integer, primary_key=True)
    orden_id = Column(Integer, ForeignKey("ordenes_compra.id"))
    fecha_despacho = Column(Date)
    fecha_estimada_llegada = Column(Date)
    descripcion = Column(Text)
    estado = Column(String(30), default="en_transito")
    orden = relationship("OrdenCompra", back_populates="pedidos_recepcion")


class Operacion(Base):
    __tablename__ = "operaciones"
    id = Column(Integer, primary_key=True)
    orden_id = Column(Integer, ForeignKey("ordenes_compra.id"), nullable=True)
    descripcion = Column(Text)
    monto = Column(Float)
    resultado = Column(String(20), default="pendiente")
    justificacion = Column(Text)
    fecha = Column(Date, nullable=False)
    fecha_resolucion = Column(Date)
    orden = relationship("OrdenCompra", back_populates="operacion")


class CuentaCorriente(Base):
    __tablename__ = "cuentas_corrientes"
    id = Column(Integer, primary_key=True)
    cliente_id = Column(Integer, ForeignKey("clientes.id"), unique=True)
    saldo = Column(Float, default=0.0)
    limite_credito = Column(Float, default=0.0)
    cliente = relationship("Cliente", back_populates="cuenta_corriente")
    transacciones = relationship("TransaccionCC", back_populates="cuenta")


class TransaccionCC(Base):
    __tablename__ = "transacciones_cc"
    id = Column(Integer, primary_key=True)
    cuenta_id = Column(Integer, ForeignKey("cuentas_corrientes.id"))
    fecha = Column(Date, nullable=False)
    tipo = Column(String(20))
    monto = Column(Float)
    descripcion = Column(Text)
    cuenta = relationship("CuentaCorriente", back_populates="transacciones")
