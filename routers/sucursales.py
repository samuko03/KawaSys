from fastapi import APIRouter, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth

router = APIRouter(prefix="/sucursales", tags=["sucursales"])
templates = Jinja2Templates(directory="templates")


@router.get("/", response_class=HTMLResponse)
def sucursales_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    sucursales = db.query(models.Sucursal).all()
    return templates.TemplateResponse(request, "sucursales/index.html", {"user": user, "sucursales": sucursales})


@router.get("/nueva", response_class=HTMLResponse)
def sucursal_form(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["sistema"])
    return templates.TemplateResponse(request, "sucursales/nueva.html", {"user": user})


@router.post("/nueva")
def sucursal_crear(
    request: Request,
    nombre: str = Form(...),
    direccion: str = Form(""),
    ciudad: str = Form(""),
    telefono: str = Form(""),
    email: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["sistema"])
    s = models.Sucursal(nombre=nombre, direccion=direccion, ciudad=ciudad, telefono=telefono, email=email)
    db.add(s)
    db.commit()
    return RedirectResponse("/sucursales/?msg=Sucursal+registrada", status_code=303)


@router.get("/{sucursal_id}/editar", response_class=HTMLResponse)
def sucursal_editar_form(request: Request, sucursal_id: int, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["sistema"])
    s = db.query(models.Sucursal).filter(models.Sucursal.id == sucursal_id).first()
    return templates.TemplateResponse(request, "sucursales/editar.html", {"user": user, "sucursal": s})


@router.post("/{sucursal_id}/editar")
def sucursal_editar(
    request: Request,
    sucursal_id: int,
    nombre: str = Form(...),
    direccion: str = Form(""),
    ciudad: str = Form(""),
    telefono: str = Form(""),
    email: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["sistema"])
    s = db.query(models.Sucursal).filter(models.Sucursal.id == sucursal_id).first()
    if s:
        s.nombre = nombre
        s.direccion = direccion
        s.ciudad = ciudad
        s.telefono = telefono
        s.email = email
        db.commit()
    return RedirectResponse("/sucursales/?msg=Sucursal+actualizada", status_code=303)
