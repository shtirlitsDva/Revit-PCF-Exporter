@echo off
setlocal
rem ============================================================================
rem deploy-install.bat - build + install the PCF exporter add-in
rem
rem For every Revit version this repo supports (2022, 2024, 2025) that is
rem installed on this machine (detected via HKLM\SOFTWARE\Autodesk\Revit\<year>):
rem   1. Build revit-pcf-exporter-<year> in Release with dotnet build.
rem   2. Copy bin\Release to %%APPDATA%%\Autodesk\Revit\Addins\<year>\PCF-Exporter\
rem   3. Write the PCF-Exporter.addin manifest next to it.
rem
rem If the target files are locked (Revit running), that version is skipped
rem with a warning - nothing is half-installed.
rem ============================================================================

set "REPO=%~dp0"
set "INSTALLED=0"
set "SKIPPED=0"
set "FAILED=0"

rem ---- sanity: warn when not deploying clean master ---------------------------
set "BRANCH="
for /f "usebackq delims=" %%B in (`git -C "%REPO%." rev-parse --abbrev-ref HEAD 2^>nul`) do set "BRANCH=%%B"
if not defined BRANCH (
    echo ERROR: "%REPO%" is not a git repository or git is not on PATH.
    exit /b 1
)
if /i not "%BRANCH%"=="master" (
    echo WARNING: current branch is "%BRANCH%", not master.
    echo          You are deploying the code as checked out right now.
)
set "DIRTY="
for /f "usebackq delims=" %%S in (`git -C "%REPO%." status --porcelain 2^>nul`) do set "DIRTY=1"
if defined DIRTY (
    echo WARNING: working tree has uncommitted changes - they WILL be deployed.
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet is not on PATH - install the .NET SDK.
    exit /b 1
)
echo.

for %%V in (2022 2024 2025) do call :deploy %%V

echo.
echo ==============================================================
echo Done. Installed: %INSTALLED%   Skipped: %SKIPPED%   Failed: %FAILED%
echo ==============================================================
if not "%FAILED%"=="0" exit /b 1
exit /b 0


rem ============================================================================
:deploy  -  %1 = Revit version year
rem ============================================================================
reg query "HKLM\SOFTWARE\Autodesk\Revit\%1" >nul 2>&1
if errorlevel 1 (
    echo [%1] Revit %1 not installed - skipping.
    set /a SKIPPED+=1
    goto :eof
)

set "SRC=%REPO%revit-pcf-exporter-%1\bin\Release"

rem Build into a CLEAN output folder - msbuild never deletes DLLs that belong
rem to since-removed references, and those stale files must not be deployed.
if exist "%SRC%" rd /s /q "%SRC%"
if exist "%SRC%" (
    echo [%1] ERROR: could not clean "%SRC%" - file locked? NOT installed.
    set /a FAILED+=1
    goto :eof
)

echo [%1] Building revit-pcf-exporter-%1 ^(Release^)...
dotnet build "%REPO%revit-pcf-exporter-%1\revit-pcf-exporter-%1.csproj" -c Release -v quiet --nologo
if errorlevel 1 (
    echo [%1] ERROR: build failed - NOT installed.
    set /a FAILED+=1
    goto :eof
)

set "ADDINSDIR=%APPDATA%\Autodesk\Revit\Addins\%1"
set "DEST=%ADDINSDIR%\PCF-Exporter"
set "MANIFEST=%ADDINSDIR%\PCF-Exporter.addin"

if not exist "%SRC%\PCF-Exporter.dll" (
    echo [%1] ERROR: build output "%SRC%\PCF-Exporter.dll" not found - NOT installed.
    set /a FAILED+=1
    goto :eof
)
if not exist "%ADDINSDIR%" mkdir "%ADDINSDIR%"

rem Clean install: remove previous copy first so stale DLLs never linger.
if exist "%DEST%" rd /s /q "%DEST%" 2>nul
if exist "%DEST%" (
    echo [%1] WARNING: "%DEST%" is locked - is Revit %1 running?
    echo [%1]          Skipped. Close Revit %1 and run this script again.
    set /a SKIPPED+=1
    goto :eof
)

echo [%1] Copying to "%DEST%"...
xcopy "%SRC%\*.*" "%DEST%\" /e /i /q /y /r >nul
if errorlevel 1 (
    echo [%1] ERROR: copy failed - NOT installed.
    set /a FAILED+=1
    goto :eof
)

> "%MANIFEST%" echo ^<?xml version="1.0" encoding="utf-8" standalone="no"?^>
>>"%MANIFEST%" echo ^<RevitAddIns^>
>>"%MANIFEST%" echo   ^<AddIn Type="Application"^>
>>"%MANIFEST%" echo     ^<Assembly^>%DEST%\PCF-Exporter.dll^</Assembly^>
>>"%MANIFEST%" echo     ^<ClientId^>3ee027f8-5bc3-4e40-93f9-680baf8477ee^</ClientId^>
>>"%MANIFEST%" echo     ^<FullClassName^>PcfExporter.App.App^</FullClassName^>
>>"%MANIFEST%" echo     ^<Name^>PCF Tools^</Name^>
>>"%MANIFEST%" echo     ^<VendorId^>MGTek^</VendorId^>
>>"%MANIFEST%" echo     ^<VendorDescription^>MGTek, info@mgtek.dk^</VendorDescription^>
>>"%MANIFEST%" echo   ^</AddIn^>
>>"%MANIFEST%" echo ^</RevitAddIns^>

rem Warn if ANOTHER manifest also loads PCF-Exporter.dll (e.g. an old
rem NsRevitAddins manifest) - Revit would load the add-in twice. Revit reads
rem manifests from both the user profile AND the machine-wide ProgramData dir.
for %%F in ("%ADDINSDIR%\*.addin" "%ProgramData%\Autodesk\Revit\Addins\%1\*.addin") do (
    if /i not "%%~nxF"=="PCF-Exporter.addin" (
        findstr /i /c:"PCF-Exporter.dll" "%%F" >nul 2>&1 && (
            echo [%1] WARNING: "%%F" also references PCF-Exporter.dll.
            echo [%1]          Remove that entry or Revit loads the add-in twice.
        )
    )
)

echo [%1] Installed.
set /a INSTALLED+=1
goto :eof
