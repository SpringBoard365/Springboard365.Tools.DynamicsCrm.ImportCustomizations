NuGet.exe pack ../Springboard365.Tools.DynamicsCrm.ImportCustomizations.csproj -Build -Symbols -Version 1.0.0

NuGet.exe push *.nupkg

pause