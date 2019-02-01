$defaultAdapter = Get-WMIObject Win32_NetworkAdapterConfiguration | ? {$_.DefaultIPGateway}
if (@($defaultAdapter).Length -ne 1) {throw "You don't have 1 default gateway, your network configuration is not supported" } 
# Route local IP address via the default gateway
route add $defaultAdapter.IPAddress[0] $defaultAdapter.DefaultIPGateway
Write-Host "Start capturing on localhost by connecting to $($defaultAdapter.IPAddress[0])"
