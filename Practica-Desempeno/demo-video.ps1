# ============================================================
#  DEMO VIDEO - Sammy Garcia - Desempeno Redes
#  Muestra en pantalla, ordenado, todo lo configurado.
#  Corre esto en la parte "pruebas CLI" del video.
# ============================================================
$ErrorActionPreference = "SilentlyContinue"
Clear-Host
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "   DESEMPENO REDES - ALUMNO: SAMMY GARCIA" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Cyan

Write-Host "`n[ IDENTIDAD / EQUIPO ]" -ForegroundColor Green
Write-Host ("  Equipo : " + $env:COMPUTERNAME)
Write-Host ("  Usuario: " + (whoami))

Write-Host "`n[ ACTIVE DIRECTORY ]" -ForegroundColor Green
$d = Get-ADDomain
Write-Host ("  Dominio : " + $d.DNSRoot + "   (NetBIOS: " + $d.NetBIOSName + ")")
Write-Host ("  Contrlr : " + (Get-ADDomainController).HostName)

Write-Host "`n[ DNS ]" -ForegroundColor Green
Get-DnsServerZone | Where-Object {$_.ZoneName -notlike "*arpa*"} |
    ForEach-Object { Write-Host ("  Zona: " + $_.ZoneName) }

Write-Host "`n[ IIS / HTTP ]" -ForegroundColor Green
Write-Host ("  Servicio W3SVC: " + (Get-Service W3SVC).Status)
$r = Invoke-WebRequest http://localhost -UseBasicParsing
Write-Host ("  http://localhost responde: HTTP " + $r.StatusCode)

Write-Host "`n[ FTP ]" -ForegroundColor Green
Write-Host ("  Servicio FTPSVC: " + (Get-Service FTPSVC).Status)
Write-Host ("  Sitio FTP      : " + (Get-Website -Name 'FTP-SammyGarcia').Name + " (" + (Get-Website -Name 'FTP-SammyGarcia').State + ")")
Write-Host ("  Usuario FTP    : " + (Get-ADUser ftpalumno).SamAccountName)

Write-Host "`n[ GPO ]" -ForegroundColor Green
$g = Get-GPO -Name 'GPO-Banner-SammyGarcia'
Write-Host ("  " + $g.DisplayName + "  -> " + $g.GpoStatus)
Write-Host ("  Enlazada al dominio: SI")

Write-Host "`n[ CONECTIVIDAD ]" -ForegroundColor Green
Write-Host ("  IP del servidor: " + (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like '192.*'}).IPAddress)
Write-Host "  Ping a si mismo:"
ping -n 2 192.168.10.10 | Select-String "Reply","Respuesta","bytes"
Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "   TODOS LOS SERVICIOS OPERATIVOS - SAMMY GARCIA" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Cyan
