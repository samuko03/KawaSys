<#
====================================================================
 SCRIPT 3 de 4  -  Crear la VM CLIENTE en VirtualBox
 Desempeno Redes - reutiliza la ISO de Windows Server 2022
--------------------------------------------------------------------
 Crea una segunda VM en la misma red interna "labnet" que el servidor.
 Despues instalas el SO igual que el servidor (Desktop Experience).
 COMO CORRERLO (PowerShell, sin admin):
   .\03-Crear-VM-Cliente.ps1
====================================================================
#>

# ============ VARIABLES ============
$Alumno  = "SammyGarcia"
$ISO     = "C:\Users\Usuario\Downloads\WindowsServer2022.iso"   # misma ISO que el server
$RAM_MB  = 3072            # 3 GB para el cliente (subi si te sobra RAM)
$CPUs    = 2
$DiscoGB = 40
# ===================================

$VBox = "C:\Program Files\Oracle\VirtualBox\VBoxManage.exe"
$VM   = "CLI-$Alumno"
$ErrorActionPreference = "Stop"

if (-not (Test-Path $VBox)) { throw "No encuentro VBoxManage en $VBox" }
if (-not (Test-Path $ISO))  { throw "No encuentro la ISO en $ISO" }

Write-Host "==> Creando VM cliente '$VM' ..." -ForegroundColor Cyan
$base  = (& $VBox list systemproperties | Select-String "Default machine folder").ToString().Split(':',2)[1].Trim()
$disco = Join-Path $base "$VM\$VM.vdi"

& $VBox createvm --name $VM --ostype "Windows2022_64" --register
& $VBox modifyvm $VM --memory $RAM_MB --cpus $CPUs --vram 32 --graphicscontroller vboxsvga
# MISMA red interna que el servidor -> se ven entre si
& $VBox modifyvm $VM --nic1 intnet --intnet1 "labnet" --cableconnected1 on
& $VBox modifyvm $VM --boot1 dvd --boot2 disk --boot3 none --boot4 none

& $VBox createhd --filename $disco --size ($DiscoGB * 1024) --format VDI
& $VBox storagectl $VM --name "SATA" --add sata --controller IntelAhci --portcount 2
& $VBox storageattach $VM --storagectl "SATA" --port 0 --device 0 --type hdd --medium $disco
& $VBox storageattach $VM --storagectl "SATA" --port 1 --device 0 --type dvddrive --medium $ISO

& $VBox startvm $VM
Write-Host "`nVM cliente '$VM' creada y encendida." -ForegroundColor Green
Write-Host "Instala Windows Server 2022 (Desktop Experience) igual que el servidor." -ForegroundColor Cyan
Write-Host "Luego, DENTRO del cliente, corre 04-Cliente-UnirDominio.ps1" -ForegroundColor Cyan
