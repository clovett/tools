param([string]$DllName)

if ($DllName -eq "")
{
  Write-Host "Usage: FindProcessByDll <name>";
  Write-Host "Finds the phone processes that have loaded the given assembly";
  Write-Host "Does a substring match on your argument so any dll containing that name is returned";
  return;
}

function GetProcessList()
{
  $result = @();
  $list = tlist -s
  $lines = $list -split '\n';
  foreach ($line in $lines)
  {
     $parts = $line.Trim() -split ' ';   
     $processId = $parts[0];
     try {
       $i = [int]$processId;
       
       if ($i -gt 0)
       {
         $result = $result + $i;
       }
     } catch {     
        Write-Host "GetProcessList exception " $_.Exception.Message;
     }
  }
  return $result;
}

$list = GetProcessList;
Write-Host "Searching" $list.Count " processes...";
foreach ($processId in $list )
{
   Write-Host $processId;
   $processInfo = tlist -s $processId;
   $loaded = $processInfo -split '\n';
   $found = "";
   $title = "";
   $lowerName = $DllName.ToLower();
   
   foreach ($dll in $loaded)
   {
      if ($title -eq "") {
          $title = $dll;
      }
      elseif ($dll.ToLower().Contains($lowerName))
      {
          $found = $dll;
          break;
      }
   }
   if ($found -ne "") {
      Write-Host "---> " $title;
      Write-Host "---> " $found;
   }
}

