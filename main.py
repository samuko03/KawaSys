from fastapi import FastAPI, Request, Form, Depends
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from starlette.middleware.sessions import SessionMiddleware
from sqlalchemy.orm import Session
from database import engine, get_db, SessionLocal
import models, auth
from routers import ventas, compras, proveedores, finanzas, sucursales, sistema

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="KawaSys")
app.add_middleware(SessionMiddleware, secret_key="kawasys-secret-2024")
app.mount("/static", StaticFiles(directory="static"), name="static")
templates = Jinja2Templates(directory="templates")

app.include_router(ventas.router)
app.include_router(compras.router)
app.include_router(proveedores.router)
app.include_router(finanzas.router)
app.include_router(sucursales.router)
app.include_router(sistema.router)


def seed_db():
    db = SessionLocal()
    try:
        if db.query(models.Usuario).count() == 0:
            sucursal_central = models.Sucursal(nombre="Administración Central", ciudad="Buenos Aires",
                                               direccion="Av. Corrientes 1234", telefono="011-4444-5555")
            sucursal1 = models.Sucursal(nombre="Sucursal Norte", ciudad="Rosario",
                                        direccion="San Martín 500", telefono="0341-123-4567")
            db.add_all([sucursal_central, sucursal1])
            db.flush()

            admin = models.Usuario(
                nombre="Admin", apellido="Sistema", email="admin@kawasys.com",
                username="admin", password_hash=auth.hash_password("admin123"),
                rol="sistema", sucursal_id=sucursal_central.id
            )
            enc_ventas = models.Usuario(
                nombre="Carlos", apellido="López", email="ventas@kawasys.com",
                username="ventas", password_hash=auth.hash_password("ventas123"),
                rol="ventas", sucursal_id=sucursal1.id
            )
            enc_compras = models.Usuario(
                nombre="María", apellido="García", email="compras@kawasys.com",
                username="compras", password_hash=auth.hash_password("compras123"),
                rol="compras", sucursal_id=sucursal_central.id
            )
            enc_finanzas = models.Usuario(
                nombre="Juan", apellido="Pérez", email="finanzas@kawasys.com",
                username="finanzas", password_hash=auth.hash_password("finanzas123"),
                rol="finanzas", sucursal_id=sucursal_central.id
            )
            db.add_all([admin, enc_ventas, enc_compras, enc_finanzas])
            db.commit()
    finally:
        db.close()


seed_db()


@app.get("/", response_class=HTMLResponse)
def index(request: Request, db: Session = Depends(get_db)):
    user = auth.get_current_user(request, db)
    if not user:
        return RedirectResponse("/login")
    return RedirectResponse("/dashboard")


@app.get("/login", response_class=HTMLResponse)
def login_form(request: Request):
    return templates.TemplateResponse(request, "login.html", {"error": None})


@app.post("/login")
def login(request: Request, username: str = Form(...), password: str = Form(...),
          db: Session = Depends(get_db)):
    user = db.query(models.Usuario).filter(models.Usuario.username == username).first()
    if not user or not auth.verify_password(password, user.password_hash):
        return templates.TemplateResponse(request, "login.html", {"error": "Usuario o contraseña incorrectos"})
    if not user.activo:
        return templates.TemplateResponse(request, "login.html", {"error": "Usuario inactivo"})
    request.session["user_id"] = user.id
    return RedirectResponse("/dashboard", status_code=303)


@app.get("/logout")
def logout(request: Request):
    request.session.clear()
    return RedirectResponse("/login")


@app.get("/dashboard", response_class=HTMLResponse)
def dashboard(request: Request, db: Session = Depends(get_db)):
    user = auth.require_login(request, db)
    stats = {
        "ventas": db.query(models.Venta).count(),
        "faltantes": db.query(models.ModeloFaltante).filter(models.ModeloFaltante.emitido == False).count(),
        "ordenes_pendientes": db.query(models.OrdenCompra).filter(models.OrdenCompra.estado == "pendiente").count(),
        "operaciones_pendientes": db.query(models.Operacion).filter(models.Operacion.resultado == "pendiente").count(),
        "proveedores": db.query(models.Proveedor).filter(models.Proveedor.activo == True).count(),
        "sucursales": db.query(models.Sucursal).filter(models.Sucursal.activa == True).count(),
    }
    ultimas_ventas = db.query(models.Venta).order_by(models.Venta.id.desc()).limit(5).all()
    return templates.TemplateResponse(request, "dashboard.html", {"user": user, "stats": stats, "ultimas_ventas": ultimas_ventas})
