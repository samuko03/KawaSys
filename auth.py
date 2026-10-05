import bcrypt
from fastapi import Request, HTTPException
from sqlalchemy.orm import Session
import models


def hash_password(password: str) -> str:
    return bcrypt.hashpw(password.encode(), bcrypt.gensalt()).decode()


def verify_password(plain: str, hashed: str) -> bool:
    return bcrypt.checkpw(plain.encode(), hashed.encode())


def get_current_user(request: Request, db: Session):
    user_id = request.session.get("user_id")
    if not user_id:
        return None
    return db.query(models.Usuario).filter(models.Usuario.id == user_id).first()


def require_login(request: Request, db: Session):
    user = get_current_user(request, db)
    if not user:
        raise HTTPException(status_code=303, headers={"Location": "/login"})
    return user


def require_role(request: Request, db: Session, roles: list):
    user = require_login(request, db)
    if user.rol not in roles:
        raise HTTPException(status_code=403, detail="Sin permisos suficientes")
    return user
