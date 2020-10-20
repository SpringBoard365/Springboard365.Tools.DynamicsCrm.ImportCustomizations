SET packageVersion=2.0.0-alpha01

SET configuration=Release
SET id="Springboard365.Tools.DynamicsCrm.ImportCustomizations";
SET author="Springboard 365 Ltd";
SET repo="https://github.com/SpringBoard365/Springboard365.Tools.DynamicsCrm.ImportCustomizations";
SET description="Import customizations application to allow for automation of Power Platform Application Lifecycle Management.";
SET tags="Springboard365BuildTool PowerPlatformBuildTool Dynamics365BuildTool DynamicsCrmBuildTool XrmBuildTool";

dotnet build ../src/CommandLine.Core.csproj -c  %configuration% -p:Version=%packageVersion% -f net462 --nologo

pause

NuGet.exe pack ../src/ImportCustomizations.nuspec -Build -symbols -Version %packageVersion% -Properties Configuration=%configuration%;id=%id%;author=%author%;repo=%repo%;description=%description%;tags=%tags%;

NuGet.exe push Springboard365.Tools.DynamicsCrm.ImportCustomizations.%packageVersion%.nupkg -Source "https://api.nuget.org/v3/index.json"

pause