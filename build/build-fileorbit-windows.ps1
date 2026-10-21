[CmdletBinding()]
param([string]$OutputDir = (Join-Path $PSScriptRoot '../Releases'))
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Build the Windows package on Windows.' }
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$name = "FileOrbit-$version-win-x64"
$stage = Join-Path ([IO.Path]::GetTempPath()) ("fileorbit-" + [Guid]::NewGuid().ToString('N'))
$publish = Join-Path $stage $name
New-Item -ItemType Directory -Force -Path $publish, $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path
$archive = Join-Path $OutputDir "$name.zip"
if (Test-Path $archive) { throw 'Output already exists.' }
foreach ($project in @('Filee.App','Filee.Cli')) {
    dotnet publish (Join-Path $root "src/$project/$project.csproj") -c Release -r win-x64 --self-contained true -p:FileeRelease=true -p:PublishReadyToRun=false -o $publish
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $project" }
}
& (Join-Path $root 'build/fetch-engines.ps1') -Destination (Join-Path $publish 'engines') -Only rhwp,7zip -RuntimeTo $publish
Rename-Item (Join-Path $publish 'Filee.exe') 'FileOrbit.exe'
foreach ($file in @('LICENSE','THIRD-PARTY-NOTICES.md')) {
    Copy-Item (Join-Path $root $file) $publish
}
Copy-Item (Join-Path $root 'docs/WINDOWS-INSTALL.txt') (Join-Path $publish 'INSTALL.txt')
Compress-Archive -LiteralPath $publish -DestinationPath $archive
"$((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant())  $name.zip" | Set-Content (Join-Path $OutputDir "$name-SHA256SUMS.txt") -Encoding ascii
Write-Host "Package: $archive"
