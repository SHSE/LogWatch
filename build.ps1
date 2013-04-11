if (!(Test-Path .\Tools\xunit\xunit.console.clr4.x86.exe)) {
    New-Item .\Tools\xunit -ItemType Directory -ErrorAction SilentlyContinue
    
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile('http://download-codeplex.sec.s-msft.com/Download/Release?ProjectName=xunit&DownloadId=423827&FileTime=129859153262930000&Build=20310', '.\Tools\xunit\xunit.zip')

    $shell_app = new-object -com shell.application
    $zip_file = $shell_app.namespace((Resolve-Path .\Tools\xunit\xunit.zip).Path)
    $destination = $shell_app.namespace((Resolve-Path .\Tools\xunit).Path)
    $destination.Copyhere($zip_file.items())
}

C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe .\LogWatch.Tests\LogWatch.Tests.csproj /p:DownloadNuGetExe=True

if ($LASTEXITCODE -ne 0) {
    throw "Failed to build tests project"
}

.\Tools\xunit\xunit.console.clr4.x86.exe .\LogWatch.Tests\bin\Debug\LogWatch.Tests.dll

if ($LASTEXITCODE -ne 0) {
    throw "Huston, we have a problem!"
}

C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe .\LogWatch\LogWatch.csproj `
    /t:publish `
    /p:Configuration=Release `
    /p:DownloadNuGetExe=True `
    /p:ApplicationVersion=$env:ApplicationVersion

if ($LASTEXITCODE -ne 0) {
    throw "Publishing failed"
}

Remove-Item .\Build -ErrorAction SilentlyContinue -Recurse -Force
New-Item .\Build -ItemType Directory -ErrorAction SilentlyContinue

Copy-Item .\LogWatch\bin\Release\app.publish\* .\Build -Recurse -Force

if (!(Test-Path .\Tools\psftp.exe)) {
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile('http://the.earth.li/~sgtatham/putty/latest/x86/psftp.exe', '.\Tools\psftp.exe')   
}

Remove-Item .\Tools\deploy.psftp -ErrorAction SilentlyContinue

$versionDirName = (Get-Item '.\Build\Application Files\LogWatch*').Name

@('cd /home/pfs/project/logwatch-dotnet',
  'del LogWatch.application',
  'del setup.exe',
  'mput *.*',
  'cd "Application Files"',
  "mkdir `"$versionDirName`"",
  "cd $versionDirName",
  "mput `"Build\Application Files\$versionDirName\*.*`"",
  'mkdir en',
  'cd en',
  "mput `"Build\Application Files\$versionDirName\en\*.*`"") `
  | Add-Content .\Tools\deploy.psftp

.\Tools\psftp.exe -l sergey-shumov frs.sourceforge.net -b .\Tools\deploy.psftp