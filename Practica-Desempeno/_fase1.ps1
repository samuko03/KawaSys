# Fase 1: IP fija + DNS propio + renombrar equipo, luego reinicia
$ErrorActionPreference = "Stop"
$IPServer = "192.168.10.10"
$Prefijo  = 24
$NombrePC = "SRV-SammyGarcia"

$ad = Get-NetAdapter | Where-Object { $_.Status -eq "Up" } | Select-Object -First 1
Remove-NetIPAddress -InterfaceIndex $ad.ifIndex -Confirm:$false -ErrorAction SilentlyContinue
Remove-NetRoute     -InterfaceIndex $ad.ifIndex -Confirm:$false -ErrorAction SilentlyContinue
New-NetIPAddress -InterfaceIndex $ad.ifIndex -IPAddress $IPServer -PrefixLength $Prefijo | Out-Null
Set-DnsClientServerAddress -InterfaceIndex $ad.ifIndex -ServerAddresses 127.0.0.1
"IP configurada: $IPServer/$Prefijo en '$($ad.Name)'"

if ($env:COMPUTERNAME -ne $NombrePC) {
    Rename-Computer -NewName $NombrePC -Force
    "Equipo renombrado a $NombrePC (se aplica al reiniciar)"
}
"Reiniciando en 5 segundos..."
Start-Sleep -Seconds 5
Restart-Computer -Force
