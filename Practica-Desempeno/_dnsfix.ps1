# Deshabilita IPv6 en el adaptador para que nslookup use IPv4 limpio
$if = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Select-Object -First 1
Disable-NetAdapterBinding -Name $if.Name -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue
Clear-DnsClientCache
Start-Sleep -Seconds 3
"=== nslookup directo ==="
nslookup sammygarcia.local
"=== nslookup inverso ==="
nslookup 192.168.10.10
