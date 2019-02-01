
$GrabDir="C:\Users\clovett\AppData\Roaming\iSpy\WebServerRoot\Media\video\XKNVK\grabs"
$a = Get-ChildItem  $GrabDir | Sort-Object CreationTime -descending

$snapshot = ""

foreach ($file in $a)
{
     $snapshot = $file.FullName;
     break;
}

Write-Host "Sending $snapshot";

$d = Get-Date;

Write-Host "sending mail $d"

.\SendEmail.exe -host smtpout.secureserver.net -port 3535 -user chris@lovettsoftware.com -password yeshua -from chris@lovettsoftware.com -to clovett@microsoft.com -subject "office motion detection" -message "Someone is in your office: $d" -attach $snapshot
