$start = [System.DateTime]::Now

$cmd = $args[0]

$cmdargs = @()
For ($i = 1; $i -lt $args.Count; $i++){
    $arg = $args[$i]
    $cmdargs += $arg
}

&$cmd $cmdargs

$end = [System.DateTime]::Now
$seconds = ($end.Subtract($start)).TotalSeconds

$seconds=($end.Subtract($start)).TotalSeconds
Write-Host "============== Total seconds: $seconds"