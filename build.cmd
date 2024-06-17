@echo off
setlocal

:: Start in the directory containing this script
cd %~dp0

set ContinuousIntegrationBuild=true

set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\amd64\MSBuild.exe"

echo ====================================================================== RESTORE
dotnet restore Bonsai.sln
::%MSBUILD% -maxcpucount -verbosity:m -nologo -target:Restore Bonsai.sln
echo ====================================================================== BUILD
::dotnet build Bonsai.sln --no-restore --configuration Release
%MSBUILD% -maxcpucount -verbosity:m -nologo -property:Configuration=Release Bonsai.sln
::echo ====================================================================== PACK
::dotnet pack Bonsai.sln --no-build --configuration Release
::::%MSBUILD% -maxcpucount -verbosity:m -nologo -target:Pack --property:_IsPacking=true -property:NoBuild=true -property:Configuration=Release -property:DOTNET_CLI_DISABLE_PUBLISH_AND_PACK_RELEASE=true Bonsai.sln
::echo ====================================================================== TEST
::dotnet test Bonsai.sln --no-restore --no-build --configuration Release --verbosity normal
::::%MSBUILD% -maxcpucount -verbosity:m -nologo -target:VSTest -nodereuse:false -property:VSTestNoBuild=true -property:Configuration=Release -property:VSTestVerbosity=n -property:VSTestArtifactsProcessingMode=collect Bonsai.sln

::echo ====================================================================== VS TOOLS
::call tooling/vs-tools.cmd

::echo ====================================================================== TEMPLATES
::%MSBUILD% Bonsai.Templates\Bonsai.Templates.sln /p:Configuration=Release /p:Platform="Any CPU"
::
::echo ====================================================================== RESTORE SETUP
::nuget restore Bonsai.Setup\packages.config -SolutionDir .
::nuget restore Bonsai.Setup.Bootstrapper\packages.config -SolutionDir .
::
::echo ====================================================================== BUILD SETUP
::%MSBUILD% Bonsai.Setup.sln /p:Configuration=Release
