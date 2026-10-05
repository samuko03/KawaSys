from fastapi import APIRouter, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth
from datetime import date

router = APIRouter(prefix="/compras", tags=["compras"])
templates = Jinja2Templates(directory="templates")


@router.get("/", response_class=HTMLResponse)
def compras_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    solicitudes = db.query(models.SolicitudCompra).order_by(models.SolicitudCompra.id.desc()).all()
    return templates.TemplateResponse(request, "compras/index.html", {"user": user, "solicitudes": solicitudes})


@router.get("/solicitudes/nueva", response_class=HTMLResponse)
def solicitud_form(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["compras", "sistema"])
    faltantes = db.query(models.ModeloFaltante).filter(models.ModeloFaltante.emitido == True).all()
    return templates.TemplateResponse(request, "compras/solicitud_nueva.html", {"user": user, "faltantes": faltantes})


@router.post("/solicitudes/nueva")
def solicitud_crear(
    request: Request,
    descripcion: str = Form(...),
    marcas: list = Form([]),
    modelos_list: list = Form([]),
    cantidades: list = Form([]),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    solicitud = models.SolicitudCompra(fecha=date.today(), descripcion=descripcion)
    db.add(solicitud)
    db.flush()
    for i in range(len(marcas)):
        if i < len(modelos_list) and i < len(cantidades):
            item = models.ItemSolicitudCompra(
                solicitud_id=solicitud.id,
                marca=marcas[i],
                modelo=modelos_list[i],
                cantidad=int(cantidades[i])
            )
            db.add(item)
    db.commit()
    return RedirectResponse("/compras/?msg=Solicitud+registrada", status_code=303)


@router.get("/cotizaciones", response_class=HTMLResponse)
def cotizaciones_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    sol_cots = db.query(models.SolicitudCotizacion).order_by(models.SolicitudCotizacion.id.desc()).all()
    solicitudes = db.query(models.SolicitudCompra).all()
    return templates.TemplateResponse(request, "compras/cotizaciones.html", {"user": user, "sol_cots": sol_cots, "solicitudes": solicitudes})


@router.post("/cotizaciones/nueva")
def cotizacion_crear(
    request: Request,
    solicitud_compra_id: int = Form(...),
    descripcion: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    sc = models.SolicitudCotizacion(
        solicitud_compra_id=solicitud_compra_id,
        fecha=date.today(),
        descripcion=descripcion
    )
    db.add(sc)
    db.commit()
    return RedirectResponse("/compras/cotizaciones?msg=Solicitud+de+cotizacion+generada", status_code=303)


@router.post("/cotizaciones/{sc_id}/modificar")
def cotizacion_modificar(
    request: Request,
    sc_id: int,
    descripcion: str = Form(""),
    estado: str = Form("enviada"),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    sc = db.query(models.SolicitudCotizacion).filter(models.SolicitudCotizacion.id == sc_id).first()
    if sc:
        sc.descripcion = descripcion
        sc.estado = estado
        db.commit()
    return RedirectResponse("/compras/cotizaciones?msg=Solicitud+modificada", status_code=303)


@router.get("/cotizaciones-recibidas", response_class=HTMLResponse)
def cotizaciones_recibidas(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    cotizaciones = db.query(models.Cotizacion).order_by(models.Cotizacion.id.desc()).all()
    sol_cots = db.query(models.SolicitudCotizacion).all()
    proveedores = db.query(models.Proveedor).filter(models.Proveedor.activo == True).all()
    return templates.TemplateResponse(request, "compras/cotizaciones_recibidas.html", {"user": user, "cotizaciones": cotizaciones,
        "sol_cots": sol_cots, "proveedores": proveedores})


@router.post("/cotizaciones-recibidas/registrar")
def cotizacion_registrar(
    request: Request,
    solicitud_cotizacion_id: int = Form(...),
    proveedor_id: int = Form(...),
    monto_total: float = Form(...),
    condiciones: str = Form(""),
    plazo_entrega: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    cot = models.Cotizacion(
        solicitud_cotizacion_id=solicitud_cotizacion_id,
        proveedor_id=proveedor_id,
        fecha=date.today(),
        monto_total=monto_total,
        condiciones=condiciones,
        plazo_entrega=plazo_entrega
    )
    db.add(cot)
    db.commit()
    return RedirectResponse("/compras/cotizaciones-recibidas?msg=Cotizacion+registrada", status_code=303)


@router.get("/ordenes", response_class=HTMLResponse)
def ordenes_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    ordenes = db.query(models.OrdenCompra).order_by(models.OrdenCompra.id.desc()).all()
    cotizaciones = db.query(models.Cotizacion).filter(models.Cotizacion.seleccionada == False).all()
    return templates.TemplateResponse(request, "compras/ordenes.html", {"user": user, "ordenes": ordenes, "cotizaciones": cotizaciones})


@router.post("/ordenes/generar")
def orden_generar(
    request: Request,
    cotizacion_id: int = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    cot = db.query(models.Cotizacion).filter(models.Cotizacion.id == cotizacion_id).first()
    orden = models.OrdenCompra(
        cotizacion_id=cotizacion_id,
        fecha_emision=date.today(),
        monto_total=cot.monto_total if cot else 0,
        observaciones=observaciones
    )
    db.add(orden)
    if cot:
        cot.seleccionada = True
    db.flush()
    operacion = models.Operacion(
        orden_id=orden.id,
        descripcion=f"Orden de compra #{orden.id}",
        monto=orden.monto_total,
        fecha=date.today()
    )
    db.add(operacion)
    db.commit()
    return RedirectResponse("/compras/ordenes?msg=Orden+generada+y+enviada+a+finanzas", status_code=303)


@router.get("/recepcion", response_class=HTMLResponse)
def recepcion_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    pedidos = db.query(models.PedidoRecepcion).order_by(models.PedidoRecepcion.id.desc()).all()
    ordenes = db.query(models.OrdenCompra).filter(models.OrdenCompra.estado == "aprobada").all()
    return templates.TemplateResponse(request, "compras/recepcion.html", {"user": user, "pedidos": pedidos, "ordenes": ordenes})


@router.post("/recepcion/registrar")
def recepcion_registrar(
    request: Request,
    orden_id: int = Form(...),
    fecha_despacho: str = Form(...),
    fecha_estimada_llegada: str = Form(...),
    descripcion: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    pedido = models.PedidoRecepcion(
        orden_id=orden_id,
        fecha_despacho=date.fromisoformat(fecha_despacho),
        fecha_estimada_llegada=date.fromisoformat(fecha_estimada_llegada),
        descripcion=descripcion
    )
    db.add(pedido)
    db.commit()
    return RedirectResponse("/compras/recepcion?msg=Pedido+registrado", status_code=303)
