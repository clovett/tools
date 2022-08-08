param(
    [Parameter(Mandatory=$true)]
    [string]$machine,
    [Parameter(Mandatory=$true)]
    [string]$name,
    [Parameter(Mandatory=$true)]
    [string]$count
)

while ($True){
    $rc = &ssh $machine ps ax
    $found = 0
    foreach ($line in $rc){
        if ($line.Contains($name)) {
            $found = $found + 1
        }
    }
    Write-Host "Found $found $name"
    if ($found -lt $count) {
        &ToastMe "$machine does not have $count instances of $name"
        Exit 1
    }
    Write-Host "Sleeping for 60 seconds..."
    Start-Sleep -Seconds 60
}