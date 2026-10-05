# Fase 2: instalar AD DS + DNS y crear el bosque/dominio (reinicia al final)
$ErrorActionPreference = "Stop"
$Dominio = "sammygarcia.local"
$Netbios = "SAMMY"
$DSRMPass = "Servidor.2024"

"Instalando rol AD DS..."
Install-WindowsFeature -Name AD-Domain-Services -IncludeManagementTools | Out-Null
"Rol instalado. Promoviendo a controlador de dominio (esto reinicia)..."
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
