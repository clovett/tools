param([Parameter(Mandatory=$True)][string]$processName)

Get-WmiObject Win32_Process -Filter "name='$processName'" | select CommandLine | out-string -Width 8000
