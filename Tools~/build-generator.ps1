$ErrorActionPreference = 'Stop'
$generatorProject = Join-Path $PSScriptRoot 'DODBT.SourceGenerator/DODBT.SourceGenerator.csproj'
dotnet build $generatorProject -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Source generator build failed.' }
dotnet run --project (Join-Path $PSScriptRoot 'DODBT.SourceGenerator.Tests/DODBT.SourceGenerator.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Source generator tests failed.' }
$pluginRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'VadimBurym-DODBT-Dots'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DODBT.SourceGenerator/bin/Release/netstandard2.0/DODBT.SourceGenerator.dll') -Destination $pluginRoot
