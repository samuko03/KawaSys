from fastapi import APIRouter, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth
from datetime import date

router = APIRouter(prefix="/finanzas", tags=["finanzas"])
templates = Jinja2Templates(directory="templates")


@router.get("/", response_class=HTMLResponse)
def finanzas_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["finanzas", "sistema"])
    pendientes = db.query(models.Operacion).filter(models.Operacion.resultado == "pendiente").all()
    return templates.TemplateResponse(request, "finanzas/index.html", {"user": user, "pendientes": pendientes})


@router.post("/operaciones/{op_id}/resolver")
def operacion_resolver(
    request: Request,
    op_id: int,
    resultado: str = Form(...),
    justificacion: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["finanzas", "sistema"])
    op = db.query(models.Operacion).filter(models.Operacion.id == op_id).first()
    if op:
        op.resultado = resultado
        op.justificacion = justificacion
        op.fecha_resolucion = date.today()
        if op.orden_id:
            orden = db.query(models.OrdenCompra).filter(models.OrdenCompra.id == op.orden_id).first()
            if orden:
                orden.estado = resultado
        db.commit()
    return RedirectResponse("/finanzas/?msg=Operacion+resuelta", status_code=303)


@router.post("/operaciones/{op_id}/modificar")
def operacion_modificar(
    request: Request,
    op_id: int,
    resultado: str = Form(...),
    justificacion: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["finanzas", "sistema"])
    op = db.query(models.Operacion).filter(models.Operacion.id == op_id).first()
    if op:
        op.resultado = resultado
        op.justificacion = justificacion
        db.commit()
    return RedirectResponse("/finanzas/historial?msg=Operacion+modificada", status_code=303)


@router.get("/historial", response_class=HTMLResponse)
def finanzas_historial(
    request: Request,
    mes: str = "",
    anio: str = "",
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["finanzas", "sistema"])
    query = db.query(models.Operacion).filter(models.Operacion.resultado != "pendiente")
    operaciones = query.order_by(models.Operacion.fecha.desc()).all()
    return templates.TemplateResponse(request, "finanzas/historial.html", {"user": user, "operaciones": operaciones,
        "mes": mes, "anio": anio})


@router.get("/reporte-mensual", response_class=HTMLResponse)
def reporte_mensual(
    request: Request,
    mes: str = "",
    anio: str = "",
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["finanzas", "sistema"])
    query = db.query(models.Operacion).filter(models.Operacion.resultado != "pendiente")
    if mes and anio:
        from sqlalchemy import extract
        query = query.filter(
            extract('month', models.Operacion.fecha) == int(mes),
            extract('year', models.Operacion.fecha) == int(anio)
        )
    operaciones = query.order_by(models.Operacion.fecha.desc()).all()
    aprobadas = [o for o in operaciones if o.resultado == "aprobada"]
    rechazadas = [o for o in operaciones if o.resultado == "rechazada"]
    total_aprobado = sum(o.monto or 0 for o in aprobadas)
    total_rechazado = sum(o.monto or 0 for o in rechazadas)
    return templates.TemplateResponse(request, "finanzas/reporte.html", {"user": user,
        "operaciones": operaciones, "aprobadas": aprobadas, "rechazadas": rechazadas,
        "total_aprobado": total_aprobado, "total_rechazado": total_rechazado,
        "mes": mes, "anio": anio})
