<#
====================================================================
 SCRIPT 0 de 3  -  Crear la VM de Windows Server 2022 en VirtualBox
 Desempeno Redes
--------------------------------------------------------------------
 QUE HACE:
   - Crea una maquina virtual con tu nombre
   - Le asigna RAM, CPU, disco de 50 GB
   - Le monta la ISO de Windows Server 2022
   - La enciende para que instales el sistema operativo
 COMO CORRERLO (PowerShell, NO hace falta admin):
   .\00-Crear-VM.ps1
 Despues instalas Windows Server a mano (ver GUIA-Practica.md, Parte 0).
====================================================================
#>

# ============ EDITA ESTAS VARIABLES ============
$Alumno  = "SammyGarcia"   # Sammy Garcia (sin espacios para nombres de equipo/red)
$ISO     = "C:\Users\Usuario\Downloads\WindowsServer2022.iso"   # ruta a la ISO
$RAM_MB  = 4096               # 4 GB de RAM (subi a 6144 si tu PC tiene >=16 GB)
$CPUs    = 2
$DiscoGB = 50
# ===============================================

$VBox = "C:\Program Files\Oracle\VirtualBox\VBoxManage.exe"
$VM   = "SRV-$Alumno"
$ErrorActionPreference = "Stop"

if (-not (Test-Path $VBox)) { throw "No encuentro VBoxManage en $VBox" }
if (-not (Test-Path $ISO))  { throw "No encuentro la ISO en $ISO (espera a que termine la descarga)" }

Write-Host "==> Creando VM '$VM' ..." -ForegroundColor Cyan

# Carpeta donde se guardan las VMs
$base = (& $VBox list systemproperties | Select-String "Default machine folder").ToString().Split(':',2)[1].Trim()
$disco = Join-Path $base "$VM\$VM.vdi"

# Crear y registrar la VM (Windows 2022 64-bit)
& $VBox createvm --name $VM --ostype "Windows2022_64" --register

# Memoria, CPU, red (NAT por defecto + permitir red interna luego), placa, video
& $VBox modifyvm $VM --memory $RAM_MB --cpus $CPUs --vram 32 --graphicscontroller vboxsvga
# Red INTERNA "labnet": el server y el futuro cliente quedan en la misma red 192.168.10.x.
# (Un controlador de dominio no necesita salida a internet para esta practica.)
& $VBox modifyvm $VM --nic1 intnet --intnet1 "labnet" --cableconnected1 on
& $VBox modifyvm $VM --boot1 dvd --boot2 disk --boot3 none --boot4 none

# Disco duro virtual
& $VBox createhd --filename $disco --size ($DiscoGB * 1024) --format VDI
& $VBox storagectl $VM --name "SATA" --add sata --controller IntelAhci --portcount 2
& $VBox storageattach $VM --storagectl "SATA" --port 0 --device 0 --type hdd --medium $disco

# Lector DVD con la ISO montada
& $VBox storageattach $VM --storagectl "SATA" --port 1 --device 0 --type dvddrive --medium $ISO

# Encender la VM
Write-Host "==> Encendiendo la VM. Segui la instalacion en la ventana de VirtualBox." -ForegroundColor Green
& $VBox startvm $VM

Write-Host "`nVM '$VM' creada y encendida." -ForegroundColor Cyan
Write-Host "Instala Windows Server 2022 'Standard Evaluation (Desktop Experience)'." -ForegroundColor Cyan
