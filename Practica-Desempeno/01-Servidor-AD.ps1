<#
====================================================================
 SCRIPT 1 de 2  -  Configuracion base + Active Directory (DNS)
 Desempeno Redes - Windows Server 2022
--------------------------------------------------------------------
 QUE HACE:
   1. Pone IP fija al servidor
   2. Renombra el equipo (con tu nombre, para acreditar identidad)
   3. Instala Active Directory + DNS y crea el dominio
   4. Reinicia automaticamente al terminar
 COMO CORRERLO:
   - Abri PowerShell COMO ADMINISTRADOR dentro de la VM
   - Permiti scripts:  Set-ExecutionPolicy Bypass -Scope Process -Force
   - Ejecuta:          .\01-Servidor-AD.ps1
   - La VM se reinicia sola. Despues logueate como Administrador del
     dominio y corre el 02-Servidor-Servicios-GPO.ps1
====================================================================
#>

# ============ EDITA ESTAS VARIABLES ============
$Alumno      = "SammyGarcia"        # Sammy Garcia (sin espacios)
$NombrePC    = "SRV-$Alumno"        # nombre de la VM/equipo (max 15 caracteres)
$Dominio     = "sammygarcia.local"  # dominio AD (lleva tu nombre = acredita identidad)
$Netbios     = "SAMMY"              # nombre NetBIOS (corto, MAYUSCULAS)
$DSRMPass    = "Servidor.2024"      # password de recuperacion de AD (DSRM)

$IPServer    = "192.168.10.10"      # IP fija del servidor
$Prefijo     = 24                   # mascara /24 = 255.255.255.0
$Gateway     = ""                   # vacio = sin gateway (red interna, no hace falta)
# ===============================================

$ErrorActionPreference = "Stop"
Write-Host "==> Alumno: $Alumno | Equipo: $NombrePC | Dominio: $Dominio" -ForegroundColor Cyan

# Recortar nombre de equipo a 15 caracteres (limite NetBIOS)
if ($NombrePC.Length -gt 15) { $NombrePC = $NombrePC.Substring(0,15) }

# --- 1. IP FIJA ---
Write-Host "`n[1/4] Configurando IP fija $IPServer/$Prefijo ..." -ForegroundColor Yellow
$ad = Get-NetAdapter | Where-Object { $_.Status -eq "Up" } | Select-Object -First 1
if (-not $ad) { throw "No se encontro un adaptador de red activo." }

# Limpia configuracion previa por las dudas
Remove-NetIPAddress -InterfaceIndex $ad.ifIndex -Confirm:$false -ErrorAction SilentlyContinue
Remove-NetRoute     -InterfaceIndex $ad.ifIndex -Confirm:$false -ErrorAction SilentlyContinue

if ([string]::IsNullOrWhiteSpace($Gateway)) {
    New-NetIPAddress -InterfaceIndex $ad.ifIndex -IPAddress $IPServer -PrefixLength $Prefijo | Out-Null
} else {
    New-NetIPAddress -InterfaceIndex $ad.ifIndex -IPAddress $IPServer -PrefixLength $Prefijo -DefaultGateway $Gateway | Out-Null
}
# El servidor se apunta a si mismo como DNS
Set-DnsClientServerAddress -InterfaceIndex $ad.ifIndex -ServerAddresses 127.0.0.1
Write-Host "    IP configurada en adaptador '$($ad.Name)'." -ForegroundColor Green

# --- 2. RENOMBRAR EQUIPO ---
Write-Host "`n[2/4] Renombrando equipo a $NombrePC ..." -ForegroundColor Yellow
if ($env:COMPUTERNAME -ne $NombrePC) {
    Rename-Computer -NewName $NombrePC -Force
    Write-Host "    Equipo renombrado (se aplica al reiniciar)." -ForegroundColor Green
} else {
    Write-Host "    El equipo ya se llama $NombrePC." -ForegroundColor Green
}

# --- 3. INSTALAR ROL AD DS + DNS ---
Write-Host "`n[3/4] Instalando rol Active Directory Domain Services + DNS ..." -ForegroundColor Yellow
Install-WindowsFeature -Name AD-Domain-Services -IncludeManagementTools | Out-Null
Write-Host "    Rol AD DS instalado." -ForegroundColor Green

# --- 4. PROMOVER A CONTROLADOR DE DOMINIO (crea el bosque) ---
Write-Host "`n[4/4] Creando el dominio $Dominio (esto reinicia la VM)..." -ForegroundColor Yellow
Import-Module ADDSDeployment
$secure = ConvertTo-SecureString $DSRMPass -AsPlainText -Force

Install-ADDSForest `
    -DomainName $Dominio `
    -DomainNetbiosName $Netbios `
    -SafeModeAdministratorPassword $secure `
    -InstallDns `
    -DomainMode "WinThreshold" `
    -ForestMode "WinThreshold" `
    -NoRebootOnCompletion:$false `
    -Force

# (La VM se reinicia automaticamente aqui)
