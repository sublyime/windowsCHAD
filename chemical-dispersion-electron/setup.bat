@echo off
echo =================================
echo Chemical Dispersion Modeling Setup
echo =================================
echo.

echo Installing dependencies...
call npm install
if %errorlevel% neq 0 (
    echo Error: Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo Building application...
call npm run build
if %errorlevel% neq 0 (
    echo Error: Failed to build application
    pause
    exit /b 1
)

echo.
echo =================================
echo Setup completed successfully!
echo =================================
echo.
echo Available commands:
echo   npm run dev    - Development mode with hot reload
echo   npm start      - Run the built application
echo   npm run dist   - Create distribution packages
echo.
echo Starting development mode...
call npm run dev