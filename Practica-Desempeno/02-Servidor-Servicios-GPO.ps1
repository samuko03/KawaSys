<#
====================================================================
 SCRIPT 2 de 2  -  Servicios (IIS + FTP) + GPO
 Desempeno Redes - Windows Server 2022
--------------------------------------------------------------------
 CORRER DESPUES DEL REINICIO, logueado como Administrador del dominio.
 QUE HACE:
   1. Instala IIS (servidor web) y publica una pagina con tu nombre
   2. Instala FTP (sobre IIS) y crea un sitio FTP funcional (bonus)
   3. Crea una GPO con banner de inicio de sesion (muy visible en video)
      y la enlaza al dominio
 COMO CORRERLO:
   - PowerShell COMO ADMINISTRADOR
   - Set-ExecutionPolicy Bypass -Scope Process -Force
   - .\02-Servidor-Servicios-GPO.ps1
====================================================================
#>

# ============ EDITA ESTAS VARIABLES (igual que en el script 1) ============
$Alumno = "SammyGarcia"        # Sammy Garcia (sin espacios)
$FtpRoot = "C:\FTPRoot"        # carpeta raiz del FTP
$FtpUser = "ftpalumno"         # usuario de prueba para el FTP
$FtpPass = "Ftp.2024Segura"    # password de ese usuario
# =========================================================================

$ErrorActionPreference = "Stop"
Write-Host "==> Configurando servicios para el alumno: $Alumno" -ForegroundColor Cyan

# --- 1. IIS (HTTP) ---
Write-Host "`n[1/3] Instalando IIS (servidor web)..." -ForegroundColor Yellow
Install-WindowsFeature -Name Web-Server -IncludeManagementTools | Out-Null

$html = @"
<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>IIS - $Alumno</title></head>
<body style='font-family:Segoe UI;text-align:center;margin-top:80px'>
  <h1>Servidor Web IIS funcionando</h1>
  <h2>Alumno: $Alumno</h2>
  <p>Desempeno Redes - Windows Server 2022</p>
  <p>Servidor: $env:COMPUTERNAME</p>
</body></html>
"@
Set-Content -Path "C:\inetpub\wwwroot\index.html" -Value $html -Encoding UTF8
# Asegura que index.html sea documento por defecto (ignora si ya existe)
Import-Module WebAdministration
try {
    Add-WebConfigurationProperty -Filter "//defaultDocument/files" -PSPath "IIS:\" -Name "." -Value @{value="index.html"}
} catch {
    Write-Host "    (index.html ya estaba como documento por defecto)" -ForegroundColor DarkGray
}
Write-Host "    IIS instalado. Pagina publicada con tu nombre." -ForegroundColor Green

# --- 2. FTP (bonus, sobre IIS) ---
Write-Host "`n[2/3] Instalando y configurando FTP..." -ForegroundColor Yellow
Install-WindowsFeature -Name Web-Ftp-Server -IncludeManagementTools | Out-Null

if (-not (Test-Path $FtpRoot)) { New-Item -ItemType Directory -Path $FtpRoot | Out-Null }
Set-Content -Path "$FtpRoot\bienvenida.txt" -Value "FTP de $Alumno - Desempeno Redes" -Encoding UTF8

# Usuario de prueba para el FTP.
# En un controlador de dominio NO existen usuarios locales -> se crea en el dominio.
$sp = ConvertTo-SecureString $FtpPass -AsPlainText -Force
$esDC = (Get-CimInstance Win32_OperatingSystem).ProductType -eq 2
if ($esDC) {
    Import-Module ActiveDirectory
    if (-not (Get-ADUser -Filter "SamAccountName -eq '$FtpUser'" -ErrorAction SilentlyContinue)) {
        New-ADUser -Name $FtpUser -SamAccountName $FtpUser -AccountPassword $sp `
            -Enabled $true -PasswordNeverExpires $true -Description "Usuario FTP $Alumno" | Out-Null
    }
} else {
    if (-not (Get-LocalUser -Name $FtpUser -ErrorAction SilentlyContinue)) {
        New-LocalUser -Name $FtpUser -Password $sp -FullName "Usuario FTP $Alumno" -Description "Prueba FTP" -PasswordNeverExpires | Out-Null
    }
}

# Crea el sitio FTP si no existe
if (-not (Get-Website -Name "FTP-$Alumno" -ErrorAction SilentlyContinue)) {
    New-WebFtpSite -Name "FTP-$Alumno" -Port 21 -PhysicalPath $FtpRoot | Out-Null
}
$site = "IIS:\Sites\FTP-$Alumno"
# Autenticacion basica ON, anonima OFF
Set-ItemProperty $site -Name ftpServer.security.authentication.basicAuthentication.enabled -Value $true
Set-ItemProperty $site -Name ftpServer.security.authentication.anonymousAuthentication.enabled -Value $false
# SSL en modo "permitido" (sin certificado obligatorio, para demo)
Set-ItemProperty $site -Name ftpServer.security.ssl.controlChannelPolicy -Value 0
Set-ItemProperty $site -Name ftpServer.security.ssl.dataChannelPolicy -Value 0
# Regla de autorizacion: permitir lectura/escritura al usuario de prueba
Add-WebConfiguration "/system.ftpServer/security/authorization" -PSPath "IIS:\" -Location "FTP-$Alumno" `
    -Value @{accessType="Allow"; users=$FtpUser; permissions="Read,Write"} -ErrorAction SilentlyContinue
Restart-WebItem $site -ErrorAction SilentlyContinue
# Abre el puerto FTP en el firewall
Enable-NetFirewallRule -DisplayGroup "Servidor FTP" -ErrorAction SilentlyContinue
Enable-NetFirewallRule -DisplayGroup "FTP Server" -ErrorAction SilentlyContinue
Write-Host "    FTP listo. Usuario: $FtpUser  Pass: $FtpPass" -ForegroundColor Green

# --- 3. GPO (banner de inicio de sesion) ---
Write-Host "`n[3/3] Creando GPO de banner de inicio de sesion..." -ForegroundColor Yellow
Import-Module GroupPolicy
$gpoName = "GPO-Banner-$Alumno"
if (-not (Get-GPO -Name $gpoName -ErrorAction SilentlyContinue)) {
    New-GPO -Name $gpoName -Comment "Banner de aviso legal - Desempeno Redes" | Out-Null
}
$reg = "HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System"
Set-GPRegistryValue -Name $gpoName -Key $reg -ValueName "legalnoticecaption" -Type String `
    -Value "AVISO - Equipo administrado por $Alumno" | Out-Null
Set-GPRegistryValue -Name $gpoName -Key $reg -ValueName "legalnoticetext" -Type String `
    -Value "Acceso restringido. Dominio administrado por el alumno $Alumno (Desempeno Redes)." | Out-Null

# Enlaza la GPO a la raiz del dominio
$dn = (Get-ADDomain).DistinguishedName
if (-not (Get-GPInheritance -Target $dn).GpoLinks.DisplayName.Contains($gpoName)) {
    New-GPLink -Name $gpoName -Target $dn -LinkEnabled Yes | Out-Null
}
gpupdate /force | Out-Null
Write-Host "    GPO '$gpoName' creada y enlazada al dominio." -ForegroundColor Green

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " LISTO. Servicios configurados:" -ForegroundColor Cyan
Write-Host "   - Active Directory + DNS (dominio creado)"
Write-Host "   - IIS  -> http://localhost  (pagina con tu nombre)"
Write-Host "   - FTP  -> ftp://localhost   (usuario $FtpUser)"
Write-Host "   - GPO  -> banner de inicio de sesion"
Write-Host " Ahora segui la GUIA-Practica.md para grabar el video." -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
