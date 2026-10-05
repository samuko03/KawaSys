from fastapi import APIRouter, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from database import get_db
import models, auth

router = APIRouter(prefix="/sistema", tags=["sistema"])
templates = Jinja2Templates(directory="templates")


@router.get("/usuarios", response_class=HTMLResponse)
def usuarios_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_role(request, db, ["sistema"])
    usuarios = db.query(models.Usuario).all()
    sucursales = db.query(models.Sucursal).filter(models.Sucursal.activa == True).all()
    return templates.TemplateResponse(request, "sistema/usuarios.html", {"user": user, "usuarios": usuarios, "sucursales": sucursales})


@router.post("/usuarios/nuevo")
def usuario_crear(
    request: Request,
    nombre: str = Form(...),
    apellido: str = Form(...),
    email: str = Form(...),
    username: str = Form(...),
    password: str = Form(...),
    rol: str = Form(...),
    sucursal_id: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_role(request, db, ["sistema"])
    u = models.Usuario(
        nombre=nombre, apellido=apellido, email=email, username=username,
        password_hash=auth.hash_password(password),
        rol=rol,
        sucursal_id=int(sucursal_id) if sucursal_id else None
    )
    db.add(u)
    db.commit()
    return RedirectResponse("/sistema/usuarios?msg=Usuario+creado", status_code=303)


@router.post("/usuarios/{user_id}/editar")
def usuario_editar(
    request: Request,
    user_id: int,
    nombre: str = Form(...),
    apellido: str = Form(...),
    email: str = Form(...),
    rol: str = Form(...),
    sucursal_id: str = Form(""),
    activo: str = Form("on"),
    db: Session = Depends(get_db)
):
    current_user = auth.require_role(request, db, ["sistema"])
    u = db.query(models.Usuario).filter(models.Usuario.id == user_id).first()
    if u:
        u.nombre = nombre
        u.apellido = apellido
        u.email = email
        u.rol = rol
        u.sucursal_id = int(sucursal_id) if sucursal_id else None
        u.activo = activo == "on"
        db.commit()
    return RedirectResponse("/sistema/usuarios?msg=Usuario+actualizado", status_code=303)


@router.get("/clientes", response_class=HTMLResponse)
def clientes_index(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    clientes = db.query(models.Cliente).all()
    return templates.TemplateResponse(request, "sistema/clientes.html", {"user": user, "clientes": clientes})


@router.post("/clientes/nuevo")
def cliente_crear(
    request: Request,
    nombre: str = Form(...),
    apellido: str = Form(""),
    dni_cuit: str = Form(""),
    telefono: str = Form(""),
    email: str = Form(""),
    direccion: str = Form(""),
    limite_credito: float = Form(0.0),
    db: Session = Depends(get_db)
):
    user = auth.require_login(request, db)
    c = models.Cliente(nombre=nombre, apellido=apellido, dni_cuit=dni_cuit,
                       telefono=telefono, email=email, direccion=direccion, tipo="mayorista")
    db.add(c)
    db.flush()
    cc = models.CuentaCorriente(cliente_id=c.id, saldo=0.0, limite_credito=limite_credito)
    db.add(cc)
    db.commit()
    return RedirectResponse("/sistema/clientes?msg=Cliente+registrado", status_code=303)


@router.post("/clientes/{cliente_id}/editar")
def cliente_editar(
    request: Request,
    cliente_id: int,
    nombre: str = Form(...),
    apellido: str = Form(""),
    dni_cuit: str = Form(""),
    telefono: str = Form(""),
    email: str = Form(""),
    direccion: str = Form(""),
    db: Session = Depends(get_db)
):
    user = auth.require_login(request, db)
    c = db.query(models.Cliente).filter(models.Cliente.id == cliente_id).first()
    if c:
        c.nombre = nombre
        c.apellido = apellido
        c.dni_cuit = dni_cuit
        c.telefono = telefono
        c.email = email
        c.direccion = direccion
        db.commit()
    return RedirectResponse("/sistema/clientes?msg=Cliente+actualizado", status_code=303)


@router.get("/clientes/{cliente_id}/cuenta", response_class=HTMLResponse)
def cuenta_corriente(request: Request, cliente_id: int, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    cliente = db.query(models.Cliente).filter(models.Cliente.id == cliente_id).first()
    cc = db.query(models.CuentaCorriente).filter(models.CuentaCorriente.cliente_id == cliente_id).first()
    transacciones = []
    if cc:
        transacciones = db.query(models.TransaccionCC).filter(
            models.TransaccionCC.cuenta_id == cc.id
        ).order_by(models.TransaccionCC.fecha.desc()).all()
    return templates.TemplateResponse(request, "sistema/cuenta_corriente.html", {"user": user, "cliente": cliente, "cc": cc, "transacciones": transacciones})


@router.post("/clientes/{cliente_id}/cuenta/transaccion")
def cuenta_transaccion(
    request: Request,
    cliente_id: int,
    tipo: str = Form(...),
    monto: float = Form(...),
    descripcion: str = Form(""),
    db: Session = Depends(get_db)
):
    from datetime import date
    user = auth.require_login(request, db)
    cc = db.query(models.CuentaCorriente).filter(models.CuentaCorriente.cliente_id == cliente_id).first()
    if cc:
        if tipo == "credito":
            cc.saldo += monto
        else:
            cc.saldo -= monto
        t = models.TransaccionCC(cuenta_id=cc.id, fecha=date.today(), tipo=tipo, monto=monto, descripcion=descripcion)
        db.add(t)
        db.commit()
    return RedirectResponse(f"/sistema/clientes/{cliente_id}/cuenta?msg=Transaccion+registrada", status_code=303)
