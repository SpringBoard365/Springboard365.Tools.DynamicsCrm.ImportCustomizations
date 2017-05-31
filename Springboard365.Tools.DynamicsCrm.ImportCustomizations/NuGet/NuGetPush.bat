SET packageVersion=1.1.0-beta4

NuGet.exe pack ../Springboard365.Tools.DynamicsCrm.ImportCustomizations.csproj -Build -symbols -Version %packageVersion% -Tool

NuGet.exe push Springboard365.Tools.DynamicsCrm.ImportCustomizations.%packageVersion%.nupkg

pause