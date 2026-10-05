from fastapi import APIRouter, Request, Form, Depends, HTTPException
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth
from datetime import date, datetime

router = APIRouter(prefix="/ventas", tags=["ventas"])
templates = Jinja2Templates(directory="templates")


@router.get("/", response_class=HTMLResponse)
def ventas_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    ventas = db.query(models.Venta).order_by(models.Venta.fecha.desc()).limit(50).all()
    return templates.TemplateResponse(request, "ventas/index.html", {"user": user, "ventas": ventas, "sucursales": sucursales})


@router.get("/registrar", response_class=HTMLResponse)
def ventas_form(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    clientes = db.query(models.Cliente).all()
    return templates.TemplateResponse(request, "ventas/registrar.html", {"user": user, "sucursales": sucursales, "clientes": clientes})


@router.post("/registrar")
def ventas_registrar(
    request: Request,
    sucursal_id: int = Form(...),
    cliente_id: str = Form(""),
    marca: str = Form(...),
    modelo: str = Form(...),
    cantidad: int = Form(...),
    precio_unitario: float = Form(...),
    forma_pago: str = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    total = cantidad * precio_unitario
    venta = models.Venta(
        fecha=date.today(),
        sucursal_id=sucursal_id,
        cliente_id=int(cliente_id) if cliente_id else None,
        marca=marca,
        modelo=modelo,
        cantidad=cantidad,
        precio_unitario=precio_unitario,
        total=total,
        forma_pago=forma_pago,
        observaciones=observaciones
    )
    db.add(venta)
    db.commit()
    return RedirectResponse("/ventas/?msg=Venta+registrada+exitosamente", status_code=303)


@router.get("/consultar", response_class=HTMLResponse)
def ventas_consultar(
    request: Request,
    sucursal_id: str = "",
    fecha_desde: str = "",
    fecha_hasta: str = "",
    db: Session = Depends(get_db)
):
    user = auth.require_login(request, db)
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    query = db.query(models.Venta)
    if sucursal_id:
        query = query.filter(models.Venta.sucursal_id == int(sucursal_id))
    if fecha_desde:
        query = query.filter(models.Venta.fecha >= fecha_desde)
    if fecha_hasta:
        query = query.filter(models.Venta.fecha <= fecha_hasta)
    ventas = query.order_by(models.Venta.fecha.desc()).all()
    return templates.TemplateResponse(request, "ventas/consultar.html", {"user": user, "ventas": ventas, "sucursales": sucursales,
        "sucursal_id": sucursal_id, "fecha_desde": fecha_desde, "fecha_hasta": fecha_hasta})


@router.get("/faltantes", response_class=HTMLResponse)
def faltantes_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    faltantes = db.query(models.ModeloFaltante).order_by(models.ModeloFaltante.id.desc()).all()
    return templates.TemplateResponse(request, "ventas/faltantes.html", {"user": user, "faltantes": faltantes, "sucursales": sucursales})


@router.post("/faltantes/generar")
def faltantes_generar(
    request: Request,
    sucursal_id: int = Form(...),
    marca: str = Form(...),
    modelo: str = Form(...),
    cantidad_solicitada: int = Form(...),
    periodo: str = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    faltante = models.ModeloFaltante(
        sucursal_id=sucursal_id, marca=marca, modelo=modelo,
        cantidad_solicitada=cantidad_solicitada, fecha_generacion=date.today(),
        periodo=periodo, observaciones=observaciones
    )
    db.add(faltante)
    db.commit()
    return RedirectResponse("/ventas/faltantes?msg=Faltante+registrado", status_code=303)


@router.post("/faltantes/{faltante_id}/modificar")
def faltantes_modificar(
    request: Request,
    faltante_id: int,
    marca: str = Form(...),
    modelo: str = Form(...),
    cantidad_solicitada: int = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    f = db.query(models.ModeloFaltante).filter(models.ModeloFaltante.id == faltante_id).first()
    if f:
        f.marca = marca
        f.modelo = modelo
        f.cantidad_solicitada = cantidad_solicitada
        f.observaciones = observaciones
        db.commit()
    return RedirectResponse("/ventas/faltantes?msg=Faltante+modificado", status_code=303)


@router.post("/faltantes/{faltante_id}/eliminar")
def faltantes_eliminar(request: Request, faltante_id: int, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    f = db.query(models.ModeloFaltante).filter(models.ModeloFaltante.id == faltante_id).first()
    if f:
        db.delete(f)
        db.commit()
    return RedirectResponse("/ventas/faltantes?msg=Faltante+eliminado", status_code=303)


@router.post("/faltantes/{faltante_id}/emitir")
def faltantes_emitir(request: Request, faltante_id: int, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    f = db.query(models.ModeloFaltante).filter(models.ModeloFaltante.id == faltante_id).first()
    if f:
        f.emitido = True
        db.commit()
    return RedirectResponse("/ventas/faltantes?msg=Listado+emitido", status_code=303)


@router.get("/pedidos", response_class=HTMLResponse)
def pedidos_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    pedidos = db.query(models.PedidoCliente).order_by(models.PedidoCliente.id.desc()).all()
    return templates.TemplateResponse(request, "ventas/pedidos.html", {"user": user, "pedidos": pedidos, "sucursales": sucursales})


@router.post("/pedidos/registrar")
def pedidos_registrar(
    request: Request,
    sucursal_id: int = Form(...),
    cliente_nombre: str = Form(...),
    marca: str = Form(...),
    modelo: str = Form(...),
    cantidad: int = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    pedido = models.PedidoCliente(
        sucursal_id=sucursal_id, cliente_nombre=cliente_nombre,
        marca=marca, modelo=modelo, cantidad=cantidad,
        fecha=date.today(), observaciones=observaciones
    )
    db.add(pedido)
    db.commit()
    return RedirectResponse("/ventas/pedidos?msg=Pedido+registrado", status_code=303)


@router.get("/envios", response_class=HTMLResponse)
def envios_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    envios = db.query(models.EnvioListado).order_by(models.EnvioListado.id.desc()).all()
    faltantes_emitidos = db.query(models.ModeloFaltante).filter(models.ModeloFaltante.emitido == True).all()
    return templates.TemplateResponse(request, "ventas/envios.html", {"user": user, "envios": envios,
        "sucursales": sucursales, "faltantes_emitidos": faltantes_emitidos})


@router.post("/envios/registrar")
def envios_registrar(
    request: Request,
    sucursal_id: int = Form(...),
    periodo: str = Form(...),
    cantidad_items: int = Form(...),
    observaciones: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["ventas", "sistema"])
    envio = models.EnvioListado(
        sucursal_id=sucursal_id, fecha_envio=date.today(),
        periodo=periodo, cantidad_items=cantidad_items, observaciones=observaciones
    )
    db.add(envio)
    db.commit()
    return RedirectResponse("/ventas/envios?msg=Envio+registrado", status_code=303)
