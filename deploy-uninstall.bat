@echo off
setlocal
rem ============================================================================
rem deploy-uninstall.bat - remove the PCF exporter add-in
rem
rem Scans every version folder under %%APPDATA%%\Autodesk\Revit\Addins\ and
rem removes PCF-Exporter.addin + the PCF-Exporter\ payload folder.
rem
rem If files are locked (that Revit version is running), the version is
rem skipped with a warning and left fully intact - manifest and payload are
rem only ever removed together, never half.
rem ============================================================================

set "ADDINS=%APPDATA%\Autodesk\Revit\Addins"
set "REMOVED=0"
set "LOCKED=0"

if not exist "%ADDINS%" (
    echo Nothing to do: "%ADDINS%" does not exist.
    exit /b 0
)

for /d %%D in ("%ADDINS%\*") do call :remove "%%D"

echo.
echo ==============================================================
echo Done. Removed: %REMOVED%   Locked/skipped: %LOCKED%
echo ==============================================================
if not "%LOCKED%"=="0" exit /b 1
exit /b 0


rem ============================================================================
:remove  -  %1 = quoted Addins\<version> folder
rem ============================================================================
set "DIR=%~1"
set "VER=%~n1"
if not exist "%DIR%\PCF-Exporter.addin" if not exist "%DIR%\PCF-Exporter\" goto :eof

rem Remove the payload first; if it is locked, leave the manifest too so the
rem installation stays consistent.
if exist "%DIR%\PCF-Exporter\" rd /s /q "%DIR%\PCF-Exporter" 2>nul
if exist "%DIR%\PCF-Exporter\" (
    echo [%VER%] WARNING: "%DIR%\PCF-Exporter" is locked - is Revit %VER% running?
    echo [%VER%]          Skipped. Close Revit %VER% and run this script again.
    set /a LOCKED+=1
    goto :eof
)

if exist "%DIR%\PCF-Exporter.addin" del /f /q "%DIR%\PCF-Exporter.addin" 2>nul
if exist "%DIR%\PCF-Exporter.addin" (
    echo [%VER%] WARNING: could not delete "%DIR%\PCF-Exporter.addin" - skipped.
    set /a LOCKED+=1
    goto :eof
)

echo [%VER%] Removed.
set /a REMOVED+=1
goto :eof
