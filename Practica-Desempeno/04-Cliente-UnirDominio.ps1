<#
====================================================================
 SCRIPT 4 de 4  -  Unir el CLIENTE al dominio
 Desempeno Redes - correr DENTRO de la VM cliente
--------------------------------------------------------------------
 REQUISITO: el servidor (SRV-SammyGarcia) debe estar ENCENDIDO,
 con AD y DNS ya configurados (scripts 01 y 02 ya corridos).
 QUE HACE:
   1. Pone IP fija al cliente y lo apunta al DNS del servidor
   2. Renombra el cliente con tu nombre
   3. Lo une al dominio sammygarcia.local (pide credenciales de admin)
   4. Reinicia
 COMO CORRERLO:
   - PowerShell COMO ADMINISTRADOR dentro del cliente
   - Set-ExecutionPolicy Bypass -Scope Process -Force
   - .\04-Cliente-UnirDominio.ps1
====================================================================
#>

# ============ VARIABLES ============
$Alumno    = "SammyGarcia"
$NombrePC  = "CLI-$Alumno"          # nombre del cliente (max 15)
$Dominio   = "sammygarcia.local"    # mismo dominio del servidor
$IPCliente = "192.168.10.20"        # IP del cliente (distinta del server .10)
$Prefijo   = 24
$DNSserver = "192.168.10.10"        # IP del servidor = DNS del dominio
# ===================================

$ErrorActionPreference = "Stop"
if ($NombrePC.Length -gt 15) { $NombrePC = $NombrePC.Substring(0,15) }
Write-Host "==> Preparando cliente '$NombrePC' para unirse a $Dominio" -ForegroundColor Cyan

# --- 1. IP fija + DNS apuntando al servidor ---
Write-Host "`n[1/3] Configurando IP $IPCliente y DNS $DNSserver ..." -ForegroundColor Yellow
$ad = Get-NetAdapter | Where-Object { $_.Status -eq "Up" } | Select-Object -First 1
if (-not $ad) { throw "No hay adaptador de red activo." }
Remove-NetIPAddress -InterfaceIndex $ad.ifIndex -Confirm:$false -ErrorAction SilentlyContinue
New-NetIPAddress -InterfaceIndex $ad.ifIndex -IPAddress $IPCliente -PrefixLength $Prefijo | Out-Null
Set-DnsClientServerAddress -InterfaceIndex $ad.ifIndex -ServerAddresses $DNSserver
Write-Host "    Listo. Probando que ve al servidor..." -ForegroundColor Green
Test-Connection -ComputerName $DNSserver -Count 2 | Format-Table -AutoSize

# --- 2. Renombrar ---
Write-Host "`n[2/3] Renombrando equipo a $NombrePC ..." -ForegroundColor Yellow
if ($env:COMPUTERNAME -ne $NombrePC) { Rename-Computer -NewName $NombrePC -Force }

# --- 3. Unir al dominio ---
Write-Host "`n[3/3] Uniendo al dominio $Dominio (ingresa: $($Dominio.Split('.')[0].ToUpper())\Administrator)..." -ForegroundColor Yellow
$cred = Get-Credential -Message "Credenciales del Administrador del dominio (ej: SAMMY\Administrator)"
Add-Computer -DomainName $Dominio -Credential $cred -Force -Restart
# (Reinicia automaticamente al unirse)
