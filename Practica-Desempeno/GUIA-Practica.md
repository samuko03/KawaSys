# Guía Práctica — 2° Desempeño Redes (Windows Server 2022)

Vos grabás el video; esto es la parte práctica ya armada. Servicios configurados:
**Active Directory + DNS, IIS (HTTP) y FTP**, más una **GPO** de banner de login.

> **IMPORTANTE (identidad):** antes de correr nada, abrí los dos `.ps1` y
> reemplazá `NombreApellido` por tu nombre real (sin espacios ni acentos).
> Eso hace que tu nombre aparezca en el equipo, el dominio, la web, el FTP y la GPO,
> que es lo que pide el instructivo para acreditar autoría.

---

## PARTE 0 — Crear la VM e instalar Windows Server (en tu PC, no dentro de la VM)

> Hacelo una sola vez. VirtualBox 7.2.8 ya está instalado y la ISO se está
> descargando a `C:\Users\Usuario\Downloads\WindowsServer2022.iso`.

1. Esperá a que termine la descarga de la ISO (~4.7 GB).
2. Editá `00-Crear-VM.ps1` y poné tu nombre en `$Alumno`.
3. En PowerShell (no hace falta admin), desde la carpeta `Practica-Desempeno`:
   ```powershell
   .\00-Crear-VM.ps1
   ```
   Crea la VM `SRV-TUNOMBRE`, le monta la ISO y la enciende.
4. **Instalá el sistema operativo** en la ventana de VirtualBox:
   - Idioma → *Next* → *Install now*.
   - Elegí **Windows Server 2022 Standard Evaluation (Desktop Experience)**
     (la opción "Desktop Experience" trae interfaz gráfica — importante).
   - Aceptá términos → *Custom* → seleccioná el disco de 50 GB → *Next*.
   - Esperá la instalación y el reinicio.
   - Poné una contraseña para el usuario **Administrator** (ej: `Servidor.2024`)
     y anotala.
5. (Recomendado) Instalá las **Guest Additions** para mejor pantalla:
   menú *Dispositivos → Insertar imagen de CD de las Guest Additions* dentro de la VM.

Cuando ya estés logueado en el escritorio de Windows Server, seguí con la Parte A.

---

## PARTE A — Ejecutar la práctica (una sola vez)

### Paso 0 — Preparar
1. La VM de Windows Server 2022 ya debe estar encendida y logueada (Parte 0).
2. Copiá la carpeta `Practica-Desempeno` adentro de la VM (carpeta compartida, o pegando el texto).
3. Abrí **PowerShell como Administrador** (clic derecho → *Ejecutar como administrador*).

### Paso 1 — Active Directory + DNS
```powershell
cd C:\ruta\donde\copiaste\Practica-Desempeno
Set-ExecutionPolicy Bypass -Scope Process -Force
.\01-Servidor-AD.ps1
```
- Pone IP fija (`192.168.10.10/24`), renombra el equipo a `SRV-TUNOMBRE`,
  instala AD + DNS y crea el dominio.
- **La VM se reinicia sola.** Es normal.

### Paso 2 — Servicios + GPO (después del reinicio)
1. Logueate como **Administrador del dominio** (ej: `KAWASYS\Administrador`).
2. PowerShell como Administrador otra vez:
```powershell
cd C:\ruta\...\Practica-Desempeno
Set-ExecutionPolicy Bypass -Scope Process -Force
.\02-Servidor-Servicios-GPO.ps1
```
- Instala IIS (con página que muestra tu nombre), FTP y crea la GPO.

✅ Con esto ya tenés todo lo obligatorio aprobado (AD + 2 servicios + GPO).

### Paso 3 — VM Cliente (para subir la nota)
> Hacelo con el **servidor encendido** y los scripts 01 y 02 ya corridos.

1. En tu PC (PowerShell, sin admin), desde `Practica-Desempeno`:
   ```powershell
   .\03-Crear-VM-Cliente.ps1
   ```
   Crea `CLI-SammyGarcia` en la misma red interna y la enciende.
2. Instalá Windows Server 2022 **(Desktop Experience)** igual que el servidor.
3. Dentro del cliente, PowerShell **como Administrador**:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force
   .\04-Cliente-UnirDominio.ps1
   ```
   Pone IP `192.168.10.20`, DNS al servidor, y une al dominio
   (te pide credenciales: `SAMMY\Administrator` + tu contraseña).
   El cliente reinicia y queda unido al dominio.
4. Logueate en el cliente con la cuenta del dominio: `SAMMY\Administrator`.

---

## PARTE B — Guion del video (máx. 2 minutos, sin editar, una sola toma)

Decí tu nombre al inicio y mostrá el nombre en pantalla. Seguí este orden y leé los
comandos en voz alta mientras se ven en pantalla.

### 1. Entorno e identidad
- Mostrá VirtualBox con la VM llamada `SRV-TUNOMBRE` corriendo.
- En el server: `hostname` y `whoami` → se ve tu nombre y el dominio.

### 2. Active Directory + Dominio
```powershell
Get-ADDomain | Select-Object Forest, NetBIOSName, DNSRoot, DistinguishedName
Get-ADUser -Filter * | Select-Object Name        # usuarios del dominio
```
- Abrí **"Usuarios y equipos de Active Directory"** (GUI) y mostrá el dominio.

### 3. DNS (servicio 1)
```powershell
Get-DnsServerZone                                 # zonas DNS
nslookup kawasys.local                            # resuelve tu dominio
```
- Opcional GUI: **Administrador de DNS** mostrando la zona directa.

### 4. IIS / HTTP (servicio 2)
```powershell
Get-Service W3SVC                                 # servicio web corriendo
```
- Abrí el navegador en `http://localhost` → se ve la página **con tu nombre**.
- GUI: **Administrador de IIS** mostrando el sitio "Default Web Site" iniciado.

### 5. FTP (servicio extra — suma nota)
```powershell
Get-Website -Name "FTP-TUNOMBRE"
```
- En el navegador o explorador: `ftp://localhost` (usuario `ftpalumno`).
- GUI: en IIS, mostrá el sitio FTP y la regla de autorización.

### 6. GPO (obligatorio)
```powershell
Get-GPO -Name "GPO-Banner-TUNOMBRE"
Get-GPInheritance -Target (Get-ADDomain).DistinguishedName   # se ve la GPO enlazada
```
- GUI: **Administración de directivas de grupo** → mostrá la GPO enlazada al dominio.
- **Impacto visible:** el banner con tu nombre aparece al cerrar/iniciar sesión.

### 7. Pruebas de conectividad (CLI — cierre fuerte)
```powershell
ipconfig /all            # IP fija + DNS apuntando al server
ping 192.168.10.10       # responde
nslookup kawasys.local   # DNS resuelve
```

### 8. Cliente unido al dominio (lo que más sube la nota) 🎯
Mostrá la VM `CLI-SammyGarcia` y, logueado como `SAMMY\Administrator`, corré:
```powershell
systeminfo | findstr /B /C:"Domain"        # confirma que pertenece a sammygarcia.local
whoami                                       # usuario del dominio
ping 192.168.10.10                           # ve al servidor
nslookup sammygarcia.local                   # el DNS del server resuelve
```
- Abrí el navegador del cliente en `http://192.168.10.10` → se ve la web con tu nombre.
- Abrí `ftp://192.168.10.10` (usuario `ftpalumno`) → accede al FTP del server.
- Cerrá/iniciá sesión para mostrar el **banner de la GPO** con tu nombre.

---

## Datos rápidos (para tener a mano en el video) — Sammy Garcia
| Cosa | Valor |
|------|-------|
| Equipo/VM | `SRV-SammyGarcia` |
| Dominio | `sammygarcia.local` (NetBIOS `SAMMY`) |
| Red | Interna "labnet", IP servidor `192.168.10.10 /24` (sin gateway) |
| Web | `http://localhost` |
| FTP | `ftp://localhost` — user `ftpalumno` / `Ftp.2024Segura` |
| GPO | `GPO-Banner-SammyGarcia` (banner de login) |
| Admin del Server | `SAMMY\Administrator` / contraseña `Samuel030405` |
| Pass DSRM (AD) | `Servidor.2024` |

> En los comandos del guion, donde dice `kawasys.local` usá `sammygarcia.local`,
> y donde dice `TUNOMBRE` usá `SammyGarcia`.

## Si algo falla
- **"no se puede correr scripts":** corré `Set-ExecutionPolicy Bypass -Scope Process -Force` y reintentá.
- **IP en conflicto / sin red:** en VirtualBox usá adaptador *Red interna* o *NAT*; ajustá `$Gateway` a la real.
- **El cliente no une al dominio:** que el DNS del cliente apunte a `192.168.10.10`.
