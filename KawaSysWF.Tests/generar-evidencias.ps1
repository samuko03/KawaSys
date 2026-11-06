# Genera las evidencias de la entrega en KawaSysWF.Tests\evidencias:
#   - kawasys_bdd.html      reporte de ejecución de los escenarios Gherkin (Reqnroll)
#   - junit-results.xml     resultados en formato JUnit, para importar en Zephyr Scale
#   - reporte_2025.png      imagen de los reportes estadísticos sobre los datos de prueba
# Uso (desde la carpeta del repo):  pwsh KawaSysWF.Tests\generar-evidencias.ps1

$ErrorActionPreference = 'Stop'
$tests = $PSScriptRoot
$salida = Join-Path $tests 'evidencias'
New-Item -ItemType Directory -Force $salida | Out-Null

dotnet test $tests --logger "junit;LogFilePath=$salida\junit-results.xml" --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) { Write-Warning "Hay escenarios con error: revisá kawasys_bdd.html" }

# Reqnroll deja sus reportes junto a los binarios de los tests.
$bdd = Join-Path $tests 'bin\Debug\net9.0-windows\reportes'
Copy-Item (Join-Path $bdd 'kawasys_bdd.html') $salida -Force
Copy-Item (Join-Path $bdd 'reporte_2025.png') $salida -Force   # lo genera el escenario "Exportar el reporte como imagen"

Write-Host "Evidencias generadas en $salida"
