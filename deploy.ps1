./build.ps1

if (!(Test-Path .\Tools\psftp.exe)) {
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile('http://the.earth.li/~sgtatham/putty/latest/x86/psftp.exe', '.\Tools\psftp.exe')   
}

Remove-Item .\Tools\deploy.psftp -ErrorAction SilentlyContinue

$versionDirName = (Get-Item '.\Build\Application Files\LogWatch*').Name

@('cd /home/pfs/project/logwatch-dotnet',
  'del LogWatch.application',
  'del setup.exe',
  'put .\Build\LogWatch.application',
  'put .\Build\setup.exe',
  'cd "Application Files"',
  "mkdir `"$versionDirName`"",
  "cd $versionDirName",
  "mput `"Build\Application Files\$versionDirName\*.*`"",
  'mkdir en',
  'cd en',
  "mput `"Build\Application Files\$versionDirName\en\*.*`"") `
  | Add-Content .\Tools\deploy.psftp

.\Tools\psftp.exe -l sergey-shumov frs.sourceforge.net -be -b .\Tools\deploy.psftp