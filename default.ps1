Framework('4.0x86')

properties {
    $version = '1.0.0.0'
    $sfpassword = $null
}

task default -depends Build

task Download-XUnit {
    if (Test-Path .\Tools\xunit\xunit.console.clr4.x86.exe) {
        return;
    }

    New-Item .\Tools\xunit -ItemType Directory -ErrorAction SilentlyContinue
    
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile('http://download-codeplex.sec.s-msft.com/Download/Release?ProjectName=xunit&DownloadId=423827&FileTime=129859153262930000&Build=20425', '.\Tools\xunit\xunit.zip')

    $shell_app = new-object -com shell.application
    $zip_file = $shell_app.namespace((Resolve-Path .\Tools\xunit\xunit.zip).Path)
    $destination = $shell_app.namespace((Resolve-Path .\Tools\xunit).Path)
    $destination.Copyhere($zip_file.items())
    
}

task Test -depends Download-XUnit {
    exec { msbuild .\LogWatch.Tests\LogWatch.Tests.csproj /p:DownloadNuGetExe=True }
    exec { .\Tools\xunit\xunit.console.clr4.x86.exe .\LogWatch.Tests\bin\Debug\LogWatch.Tests.dll /teamcity }
}

task Clean {
    Remove-Item .\Build -ErrorAction SilentlyContinue -Recurse -Force
    New-Item .\Build -ItemType Directory -ErrorAction SilentlyContinue
}

task Publish -depends Clean, Test {
    exec { msbuild .\LogWatch\LogWatch.csproj `
        /t:publish `
        /p:Configuration=Release `
        /p:DownloadNuGetExe=True `
        /p:ApplicationVersion=$version }

    Copy-Item .\LogWatch\bin\Release\app.publish\* .\Build -Recurse -Force
}

task Build -depends Clean, Test {
    exec { msbuild .\LogWatch\LogWatch.csproj `
        /t:Build `
        /p:OutputPath=..\Build\ `
        /p:Configuration=Release `
        /p:DownloadNuGetExe=True `
        /p:ApplicationVersion=$version }
}

task Download-PSFTP {
    if (Test-Path .\Tools\psftp.exe) {
        return;
    }

    New-Item .\Tools -ItemType Directory -ErrorAction SilentlyContinue

    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile('http://the.earth.li/~sgtatham/putty/latest/x86/psftp.exe', '.\Tools\psftp.exe')   
}

task Deploy -depends Publish, Download-PSFTP -precondition { $sfpassword -ne $null } {
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

    exec { .\Tools\psftp.exe -l sergey-shumov frs.sourceforge.net -be -b .\Tools\deploy.psftp -pw $sfpassword }
}