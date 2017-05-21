NuGet.exe pack ../Springboard365.Tools.DynamicsCrm.ImportCustomizations.csproj -Build -Symbols

NuGet.exe push *.nupkg

pause