@echo off
echo ============================================
echo   SmartZip Builder
echo ============================================
echo.
dotnet build SmartZip.csproj -c Release
echo.
IF %ERRORLEVEL% EQU 0 (
    echo ============================================
    echo   BUILD SUCCESSFUL
    echo ============================================
    echo Output: bin\Release\net10.0-windows\SmartZip.dll
    echo.
    echo To run: dotnet SmartZip.dll
) ELSE (
    echo ============================================
    echo   BUILD FAILED. See errors above.
    echo ============================================
)
pause
