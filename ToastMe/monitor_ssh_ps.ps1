param(
    [Parameter(Mandatory=$true)]
    [string]$machine,
    [Parameter(Mandatory=$true)]
    [string]$name,
    [Parameter(Mandatory=$true)]
    [string]$count
)

while ($True){
    $rc = &ssh $machine ps ax | grep $name
    $len = $rc.Length
    if ($rc.Length -lt $count) {
        &ToastMe "$machine does not have $count instances of $name"
        Exit 1
    }
    Write-Host "Sleeping for 60 seconds..."
    Start-Sleep -Seconds 60
}