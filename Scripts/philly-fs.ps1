# To enable Verbose statements, please run the following code before executing this script: $VerbosePreference = 'continue'
Param(
    [Parameter(Mandatory = $False)]
    [Alias("p")]
    [switch]
    $preview = $False,
    [Parameter(Mandatory = $False)]
    [Alias("z")]
    [switch]
    $usezip = $True,
    [Parameter(Mandatory = $False)]
    [Alias("i")]
    [switch]
    $information = $False,
    [Parameter(Mandatory = $False)]
    [Alias("d")]
    [switch]
    $debugOutput = $False,
    [Parameter(Mandatory = $False)]
    [Alias("v")]
    [switch]
    $verboseOutput = $False,
    [array]
    [Parameter(ValueFromRemainingArguments = $True)]
    $getRemaining
)

$VersionApi = "https://philly/api/philly-fs/version?client=windows&preview={0}"
$DownloadApi = "https://philly/api/philly-fs/download?client=windows&&preview={0}&usezip={1}&version={2}"
$PhillyFsPathExpirationTimeSpan = New-TimeSpan -Days 1
Add-Type -AssemblyName System.IO.Compression.FileSystem

class PhillyFsPathManager {
    [String] $PhillyFsRootFolderPath
    [String] $PhillyFsPreviewRootFolderPath
    [String] $PhillyFsVerFileName
    [String] $PhillyFsZipVerFileName
    [String] $PhillyFsTimestampFileName
    [String] $PhillyFsZipTimestampFileName
    [String] $PhillyFsTimestampFormat
    [String] $PhillyFsExeFileName
    [String] $PhillyFsZipFileName

    PhillyFsPathManager(){
        $this.PhillyFsRootFolderPath = Join-Path $ENV:LOCALAPPDATA -ChildPath "philly-fs"
        $this.PhillyFsPreviewRootFolderPath = Join-Path $ENV:LOCALAPPDATA -ChildPath "philly-fs-preview"
        $this.PhillyFsVerFileName = "version.txt"
        $this.PhillyFsZipVerFileName = "onefolder-version.txt"
        $this.PhillyFsTimestampFormat = "dd-MM-yyyy-hh:mm:ss"
        $this.PhillyFsTimestampFileName = "timestamp.txt"
        $this.PhillyFsZipTimestampFileName = "onefolder-timestamp.txt"
        $this.PhillyFsExeFileName = "philly-fs.exe"
        $this.PhillyFsZipFileName = "philly-fs.zip"
    }
    
    [String] GetLocalVersion([bool] $preview, [bool] $usezip) {
        $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsVerFileName
        if ($usezip) {
            $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsZipVerFileName
        }

        $localVersion = $null
        if ( Test-Path $path ) {
            $localVersion = Get-Content $path
        }
        
        return $localVersion
    }

    [Void] UpdateLocalVersion([bool] $preview, [bool] $usezip, [String] $version) {
        $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsVerFileName
        if ($usezip) {
            $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsZipVerFileName
        }

        if ( !(Test-Path $path) ) {
            New-Item -path $path -type "file" -value $version
        } else 
        {
            Set-Content -path $path -value $version
        }
    }
    
    [DateTime] GetLocalTimeStamp([bool] $preview, [bool] $usezip) {
        $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsTimestampFileName
        if ($usezip) {
            $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsZipTimestampFileName
        }

        if ( Test-Path $path ) {
            $localTimeStamp = Get-Content $path
            return [datetime]::ParseExact($localTimeStamp, $this.PhillyFsTimestampFormat, [Globalization.CultureInfo]::InvariantCulture)
        }

        # Default value 
        return (Get-Date).AddDays(-2)
    }

    [Void] UpdateLocalTimeStamp([bool] $preview, [bool] $usezip, [DateTime]$timestamp) {
        $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsTimestampFileName
        if ($usezip) {
            $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $this.PhillyFsZipTimestampFileName
        }

        if ( !(Test-Path $path )) {
            New-Item -path $path -type "file" -value $timestamp.ToString($this.PhillyFsTimestampFormat)
        } else {
            Set-Content -path $path -value $timestamp.ToString($this.PhillyFsTimestampFormat)
        }
    }
    
    [String] GetPhillyFsRootPath([bool] $preview) {
        if ( $preview) {
            return $this.PhillyFsPreviewRootFolderPath
        }

        return $this.PhillyFsRootFolderPath
    }
    
    [String] GetPhillyFsExeFolderPath([bool] $preview, [bool] $usezip, [String] $version) {
        $path = Join-Path $this.GetPhillyFsRootPath($preview) -ChildPath $version

        if ( $usezip ) {
            $path = Join-Path $path -ChildPath "philly-fs"
        }

        return $path
    }
    
    [String] GetPhillyFsExeFullName([bool] $preview, [bool] $usezip, [String] $version) {
        return Join-Path $this.GetPhillyFsExeFolderPath($preview, $usezip, $version) -ChildPath $this.PhillyFsExeFileName
    }

    [String] GetPhillyFsDownloadFileName([bool] $preview, [bool] $usezip, [String] $version) {
        $path = $this.GetPhillyFsExeFolderPath($preview, $usezip, $version)

        if( $usezip ) {
            return Join-Path $path  -ChildPath $this.PhillyFsZipFileName
        }

        return Join-Path $path  -ChildPath $this.PhillyFsExeFileName
    }

    [String] GetCachedPhillyFsExePath([bool] $preview, [bool] $usezip) {
        if ($preview) {
            if ($usezip) {
                return $ENV:PhillyFsPreviewExePath
            } else {
                return $ENV:PhillyFsPreviewZipExePath
            }
        } else {
            if ($usezip) {
                return $ENV:PhillyFsExePath
            } else {
                return $ENV:PhillyFsZipExePath
            }
        }
    }
    
    [Void] UpdateCachedPhillyFsExePath([bool] $preview, [bool] $usezip, [String] $exepath) {
        if ($preview) {
            if ($usezip) {
                $ENV:PhillyFsPreviewExePath = $exepath
            } else {
                $ENV:PhillyFsPreviewZipExePath = $exepath
            }
        } else {
            if ($usezip) {
                $ENV:PhillyFsExePath = $exepath
            } else {
                $ENV:PhillyFsZipExePath = $exepath
            }
        }
    }
}

function Unzip
{
    <#
        .DESCRIPTION
            This function unzip specified file to specified folder.
        
        .PARAMETER zipfile
            Specify the actual zip file path
        .PARAMETER outpath
            Specify where you want to unzip
        .EXAMPLE
            Unzip -zipfile c:\philly-fs.zip -outpath c:\temp
    #>
    param(
        [Parameter(Mandatory = $True)]
        [String]
        $zipfile,
        [Parameter(Mandatory = $True)]
        [String]
        $outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function Invoke-WebRequestWithRetry {
    <#
        .DESCRIPTION
            This function performs web request with retry and returns the response. 
            If web request fails each time, then it will throw the final exception.
        
        .PARAMETER Uri
            Specify the Uri to fetch
            
        .PARAMETER Retries (Optional)
            Specify number of times to perform operation.
            
        .PARAMETER OutFile (Optional)
            Specify the outfile to specify in the web request
        
        .EXAMPLE
            Invoke-WebRequestWithRetry -Uri https://philly/api/philly-fs/version?client=windows -Retries 3
    #>
    
    param(
        [Parameter(Mandatory = $True)]
        [String]
        $Uri,
        
        [int]
        $Retries = 1,
        
        [String]
        $OutFile
    )
    
    if ($Retries -le 0) {
        return $null
    }
    
    $RetryCount = 0
    do {
        try {
            $ProgressPreference = 'SilentlyContinue'
            if ($OutFile) {
                return Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseDefaultCredentials
            } else {
                return Invoke-WebRequest -Uri $Uri -UseDefaultCredentials
            }
        } catch {
             if (($RetryCount + 1) -eq $Retries) {
                throw
             }
        }
        
        $RetryCount++
    } while ($RetryCount -lt $Retries)
}

function Install-PhillyFs {
    <#
        .DESCRIPTION
            This function downloads and installs the specified version of philly-fs.

        .PARAMETER Version
            The version of philly-fs.exe to download.
        .PARAMETER preview
            Specify if it is preview version or not
        .PARAMETER usezip
            Specify if use the zip or not
        .PARAMETER fpm
            Specify the philly-fs path maanger
        .EXAMPLE
            Install-PhillyFs -version 1.0.11 -preview $False -usezip $usezip -fpm $phillyFsPathManager
    #>
    param(
        [Parameter(Mandatory = $True)]
        [String]
        $version,
        [Parameter(Mandatory = $True)]
        [bool]
        $preview,
        [Parameter(Mandatory = $True)]
        [bool]
        $usezip,
        [Parameter(Mandatory = $True)]
        [PhillyFsPathManager]
        $fpm
    )

    # generating folder path where philly-fs.exe path should be downloaded to
    $phillyFsExeFolderPath = $fpm.GetPhillyFsExeFolderPath($preview, $usezip, $version)
    if (Test-Path $phillyFsExeFolderPath) {
        Write-Warning "Deleting existing $phillyFsExeFolderPath..."
        Remove-Item -Force -Recurse $phillyFsExeFolderPath
    }

    Write-Verbose "Creating folder where philly-fs.exe will be downloaded to..."
    New-Item -ItemType Directory -Path $phillyFsExeFolderPath > $null

    # downloading philly-fs.exe
    try {
        $api = $DownloadApi -f $preview,$usezip,$version
        # phillyFsFullDowloadFileName will be varies from philly-fs.exe to philly-fs.zip based on use zip or not
        $phillyFsFullDowloadFileName = $fpm.GetPhillyFsDownloadFileName($preview, $usezip, $version)

        Write-Progress -Activity Downloading -Status 'Progress ->' -PercentComplete 1
        $response = Invoke-WebRequestWithRetry -Uri $api -OutFile $phillyFsFullDowloadFileName -Retries 5
        Write-Progress -Activity Downloading -Completed

        # updating version.txt file
        Write-Verbose "Updating version.txt..."
        $fpm.UpdateLocalVersion($preview, $usezip, $version)

        # unzip it if we download a zip file
        if ( $usezip ) {
            Write-Progress -Activity Unzip -Status 'Progress ->' -PercentComplete 1
            Unzip -zipfile $phillyFsFullDowloadFileName -outpath $phillyFsExeFolderPath
            Write-Progress -Activity Unzip --Completed
        }

        return $fpm.GetPhillyFsExeFullName($preview, $usezip, $version)
    }
    catch {
        $response = $_.Exception.Response
        Write-Warning ([String]::Format("Downloading of philly-fs.exe failed! Http Status Code: {0}", $response.StatusCode))
    }

    return $null
}

function Invoke-PhillyFsUpdateJob {
    <#
        .DESCRIPTION
            This function fetches latest version of philly-fs and compares it to the local version. If the versions don't match, then it downloads the latest version prior to executing philly-fs.exe.
        .PARAMETER preview
            Specify if it is preview version or not
        .PARAMETER usezip
            Specify if use the zip or not
        .PARAMETER fpm
            Specify the philly-fs path maanger
        .EXAMPLE
            Invoke-PhillyFsUpdateJob -preview $False -usezip $False -fpm $phillyFsPathManager
    #>
    param(
        [Parameter(Mandatory = $True)]
        [bool]
        $preview,
        [Parameter(Mandatory = $True)]
        [bool]
        $usezip,
        [Parameter(Mandatory = $True)]
        [PhillyFsPathManager]
        $fpm
    )

    # fetching local version
    $localVersion = $fpm.GetLocalVersion($preview, $usezip)

    $phillyFsExePath = $null
    if ($localVersion) {
        $phillyFsExePath = $fpm.GetPhillyFsExeFullName($preview, $usezip, $localVersion)
    }

    # fetching latest version from REST API
    Write-Verbose "Fetching latest version of philly-fs..."
    try {
        $versionUrl = $VersionApi -f $preview
        $response = Invoke-WebRequestWithRetry -Uri $versionUrl -Retries 5

        # comparing version and downloading philly-fs if necessary
        $latestVersion = $response.Content
        Write-Verbose "Lastest version available is: $latestVersion"

        if ($localVersion -and 
            ($localVersion -eq $latestVersion) -and
            (Test-Path $phillyFsExePath)) {
            Write-Verbose "Using local philly-fs.exe: $phillyFsExePath..."
        } else {
            $phillyFsExePath = Install-PhillyFs -preview $preview -usezip $usezip -version $latestVersion -fpm $fpm
            if ($phillyFsExePath) {
                Write-Verbose "Using newly downloaded philly-fs.exe: $phillyFsExePath..."
            }
        }
    } catch {
        $response = $_.Exception.Response
        if ($response) {
            Write-Warning ([String]::Format("Fetching latest version failed! Http Status Code: {0}", $response.StatusCode))
        }
    }

    if (-not $phillyFsExePath) {
        throw New-Object System.Exception("No philly-fs.exe was found on disk and downloading latest version of philly-fs.exe failed!")
    }

    $fpm.UpdateCachedPhillyFsExePath($preview, $usezip, $phillyFsExePath)
    return $phillyFsExePath
}

# we cache the timestamp when we last checked latest version of philly-fs
# we should fetch the latest version after PhillyFsPathExpirationTimeSpan

$phillyFsPathManager = [PhillyFsPathManager]::new()
$versionDateTime = $phillyFsPathManager.GetLocalTimeStamp($preview, $usezip)
$phillyFsExePath = $phillyFsPathManager.GetCachedPhillyFsExePath($preview, $usezip)

if (-not $phillyFsExePath -or 
    -not (Test-Path $phillyFsExePath) -or 
    -not $versionDateTime -or 
    (((Get-Date) - (Get-Date -Date $versionDateTime)) -gt $PhillyFsPathExpirationTimeSpan)) {
    $phillyFsExePath = Invoke-PhillyFsUpdateJob -preview $preview -usezip $usezip -fpm $phillyFsPathManager
} else {
    Write-Verbose "Using cached path!"
}

Write-Verbose "Executing $phillyFsExePath $getRemaining..."

if ($verboseOutput) {
    $getRemaining = "-v " + $getRemaining
}

if ($information) {
    $getRemaining = "-i " + $getRemaining
}

if ($debugOutput) {
    $getRemaining = "-d " + $getRemaining
}

& $phillyFsExePath ‘--%’ $getRemaining

