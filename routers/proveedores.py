from fastapi import APIRouter, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth

router = APIRouter(prefix="/proveedores", tags=["proveedores"])
templates = Jinja2Templates(directory="templates")


@router.get("/", response_class=HTMLResponse)
def proveedores_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    proveedores = db.query(models.Proveedor).filter(models.Proveedor.activo == True).all()
    return templates.TemplateResponse(request, "proveedores/index.html", {"user": user, "proveedores": proveedores})


@router.get("/nuevo", response_class=HTMLResponse)
def proveedor_form(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["compras", "sistema"])
    return templates.TemplateResponse(request, "proveedores/nuevo.html", {"user": user})


@router.post("/nuevo")
def proveedor_crear(
    request: Request,
    razon_social: str = Form(...),
    cuit: str = Form(""),
    tipo: str = Form("fijo"),
    telefono: str = Form(""),
    email: str = Form(""),
    direccion: str = Form(""),
    marcas: str = Form(""),
    modelos: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    p = models.Proveedor(
        razon_social=razon_social, cuit=cuit, tipo=tipo,
        telefono=telefono, email=email, direccion=direccion,
        marcas=marcas, modelos=modelos
    )
    db.add(p)
    db.commit()
    return RedirectResponse("/proveedores/?msg=Proveedor+registrado", status_code=303)


@router.get("/{proveedor_id}/editar", response_class=HTMLResponse)
def proveedor_editar_form(request: Request, proveedor_id: int, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["compras", "sistema"])
    p = db.query(models.Proveedor).filter(models.Proveedor.id == proveedor_id).first()
    return templates.TemplateResponse(request, "proveedores/editar.html", {"user": user, "proveedor": p})


@router.post("/{proveedor_id}/editar")
def proveedor_editar(
    request: Request,
    proveedor_id: int,
    razon_social: str = Form(...),
    cuit: str = Form(""),
    tipo: str = Form("fijo"),
    telefono: str = Form(""),
    email: str = Form(""),
    direccion: str = Form(""),
    marcas: str = Form(""),
    modelos: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["compras", "sistema"])
    p = db.query(models.Proveedor).filter(models.Proveedor.id == proveedor_id).first()
    if p:
        p.razon_social = razon_social
        p.cuit = cuit
        p.tipo = tipo
        p.telefono = telefono
        p.email = email
        p.direccion = direccion
        p.marcas = marcas
        p.modelos = modelos
        db.commit()
    return RedirectResponse("/proveedores/?msg=Proveedor+actualizado", status_code=303)


@router.get("/buscar", response_class=HTMLResponse)
def proveedores_buscar(
    request: Request,
    marca: str = "",
    modelo: str = "",
    tipo: str = "",
    db: Session = Depends(get_db)
):
    user = auth.require_login(request, db)
    query = db.query(models.Proveedor).filter(models.Proveedor.activo == True)
    if marca:
        query = query.filter(models.Proveedor.marcas.ilike(f"%{marca}%"))
    if modelo:
        query = query.filter(models.Proveedor.modelos.ilike(f"%{modelo}%"))
    if tipo:
        query = query.filter(models.Proveedor.tipo == tipo)
    proveedores = query.all()
    return templates.TemplateResponse(request, "proveedores/buscar.html", {"user": user, "proveedores": proveedores,
        "marca": marca, "modelo": modelo, "tipo": tipo})
