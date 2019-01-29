 Find the network configuration that has the default gateway.
$defaultAdapter = Get-WMIObject Win32_NetworkAdapterConfiguration | ? {$_.DefaultIPGateway}
if (@($defaultAdapter).Length -ne 1) {throw "You don't have 1 default gateway, your network configuration is not supported" } 
 
# Stop routing localhost traffic to the router.
route delete $defaultAdapter.IPAddress[0]
