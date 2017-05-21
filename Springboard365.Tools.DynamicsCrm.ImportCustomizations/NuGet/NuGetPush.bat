NuGet.exe pack ../Springboard365.Tools.DynamicsCrm.ImportCustomizations.csproj -Build -Symbols -Version 1.1.0-beta2

NuGet.exe push *.nupkg

pause