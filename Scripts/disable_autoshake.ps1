# Install-Module -Name PolicyFileEditor 

$UserDir = "$env:windir\system32\GroupPolicy\User\registry.pol"
$RegPath = "Software\Policies\Microsoft\Windows\Explorer"
$RegName = "NoWindowMinimizingShortcuts"
$RegData = 0
$RegType = "DWord"

$x = Get-PolicyFileEntry -Path $UserDir -Key $RegPath -ValueName $RegName
if ($x.Data -ne 0) {
   Write-Host "disabling NoWindowMinimizingShortcuts..."
   Set-PolicyFileEntry -Path $UserDir -Key $RegPath -ValueName $RegName -Data $RegData -Type $RegType
   Write-Host "done"
}
else
{
    Write-Host $x
    Write-Host "already disabled"
}
