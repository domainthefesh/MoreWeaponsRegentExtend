param(
    [string]$GodotSdkPath = (Join-Path $env:USERPROFILE '.nuget/packages/godot.net.sdk/4.5.1/Sdk'),
    [string]$Sts2Dir = 'D:\steam\steamapps\common\Slay the Spire 2',
    [string]$GodotExe = ''
)

$ErrorActionPreference = 'Stop'
$taskProjectRoot = Split-Path $PSScriptRoot -Parent
$taskBuildRoot = Join-Path $taskProjectRoot '.build'
$taskPackageRoot = Join-Path $taskBuildRoot 'package/MoreWeaponsRegentExtend'
$taskSavedEnvironment = @{}
$taskEnvironment = @{
    NUGET_SCRATCH = (Join-Path $taskBuildRoot 'nuget-scratch')
    TEMP = (Join-Path $taskBuildRoot 'tmp')
    TMP = (Join-Path $taskBuildRoot 'tmp')
    DOTNET_CLI_HOME = (Join-Path $taskBuildRoot 'dotnet-home')
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
    APPDATA = (Join-Path $taskBuildRoot 'user-data')
    LOCALAPPDATA = (Join-Path $taskBuildRoot 'user-data/local')
}

New-Item -ItemType Directory -Path $taskPackageRoot,$taskEnvironment.NUGET_SCRATCH,$taskEnvironment.TEMP,$taskEnvironment.DOTNET_CLI_HOME -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $taskBuildRoot '.gdignore'), '')
try {
    foreach ($taskKey in $taskEnvironment.Keys) {
        $taskSavedEnvironment[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process')
        [Environment]::SetEnvironmentVariable($taskKey, $taskEnvironment[$taskKey], 'Process')
    }
    Push-Location $taskProjectRoot
    try {
        if (!(Test-Path -LiteralPath (Join-Path $GodotSdkPath 'Sdk.props'))) {
            throw "Godot 4.5.1 SDK cache not found: $GodotSdkPath"
        }
        $taskRootXml = [System.Security.SecurityElement]::Escape($taskProjectRoot)
        $taskSdkXml = [System.Security.SecurityElement]::Escape($GodotSdkPath)
        $taskSourceXml = [System.IO.File]::ReadAllText((Join-Path $taskProjectRoot 'MoreWeaponsRegentExtend.csproj'))
        $taskOpening = '<Project><PropertyGroup><GodotProjectDir>' + $taskRootXml + '/</GodotProjectDir><AssemblyName>MoreWeaponsRegentExtend</AssemblyName><RootNamespace>MoreWeaponsRegentExtend</RootNamespace><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><Import Project="' + $taskSdkXml + '/Sdk.props" />'
        $taskClosing = '<Import Project="' + $taskRootXml + '/.godot/mono/temp/obj/MoreWeaponsRegentExtend.csproj.nuget.g.props" /><ItemGroup><Compile Include="' + $taskRootXml + '/Scripts/**/*.cs" /></ItemGroup><Import Project="' + $taskSdkXml + '/Sdk.targets" /></Project>'
        $taskLocalXml = $taskSourceXml.Replace('<Project Sdk="Godot.NET.Sdk/4.5.1">', $taskOpening).Replace('</Project>', $taskClosing)
        $taskLocalProject = Join-Path $taskBuildRoot 'MoreWeaponsRegentExtend.local.csproj'
        [System.IO.File]::WriteAllText($taskLocalProject, $taskLocalXml)
        & dotnet build $taskLocalProject --no-restore "-p:Sts2Dir=$Sts2Dir" '-p:RitsuLibAutoCopy=false' '-p:DeployToGame=false'
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

        Copy-Item -LiteralPath (Join-Path $taskProjectRoot '.godot/mono/temp/bin/Debug/MoreWeaponsRegentExtend.dll') -Destination $taskPackageRoot
        Copy-Item -LiteralPath (Join-Path $taskProjectRoot 'MoreWeaponsRegentExtend.json') -Destination $taskPackageRoot
        if ($GodotExe) {
            $taskNativeProject = Join-Path $taskBuildRoot 'resource-pack'
            New-Item -ItemType Directory -Path $taskNativeProject -Force | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $taskNativeProject 'project.godot'), "config_version=5`n[application]`nconfig/name=`"MoreWeaponsResourcePack`"`n")
            $taskPackPath = Join-Path $taskPackageRoot 'MoreWeaponsRegentExtend.pck'
            & $GodotExe --headless --path $taskNativeProject --script (Join-Path $taskProjectRoot 'tools/pack-resources.gd') -- $taskProjectRoot $taskPackPath
            if ($LASTEXITCODE -ne 0) { throw 'Resource pack failed. Import project textures in Godot first.' }
            Copy-Item -LiteralPath $taskPackPath -Destination (Join-Path $taskProjectRoot 'MoreWeaponsRegentExtend.pck')
        }
        Write-Output "Local package: $taskPackageRoot"
    }
    finally { Pop-Location }
}
finally {
    foreach ($taskKey in $taskSavedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($taskKey, $taskSavedEnvironment[$taskKey], 'Process')
    }
}
