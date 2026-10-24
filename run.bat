@echo off
setlocal
cd /d "%~dp0"
title Products API

echo ==== Products API launcher ====

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] .NET SDK not found. Install the .NET 8 SDK from https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

set "PATH=%PATH%;%USERPROFILE%\.dotnet\tools"

dotnet ef --version >nul 2>nul
if errorlevel 1 (
  echo [1/5] Installing dotnet-ef tool...
  dotnet tool install --global dotnet-ef --version 8.0.11
  if errorlevel 1 (
    echo [ERROR] Could not install dotnet-ef.
    pause
    exit /b 1
  )
) else (
  echo [1/5] dotnet-ef already installed.
)

echo [2/5] Building solution...
dotnet build Products.sln -c Debug --nologo -v q
if errorlevel 1 (
  echo [ERROR] Build failed. See messages above.
  pause
  exit /b 1
)

if not exist "Products.Infrastructure\Persistence\Migrations" (
  echo [3/5] Creating initial migration...
  dotnet ef migrations add InitialCreate -p Products.Infrastructure -s Products.API -o Persistence/Migrations
  if errorlevel 1 (
    echo [ERROR] Migration creation failed.
    pause
    exit /b 1
  )
) else (
  echo [3/5] Migration already exists.
)

echo [4/5] Updating database...
dotnet ef database update -p Products.Infrastructure -s Products.API
if errorlevel 1 (
  echo [ERROR] Database update failed. Check the connection string in Products.API\appsettings.json
  echo         and that SQL Server / LocalDB is installed and running.
  pause
  exit /b 1
)

echo [5/5] Starting API on http://localhost:5080 (JSON endpoint: /api/products) ...
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5080"
start "" cmd /c "timeout /t 8 /nobreak >nul & start http://localhost:5080/api/products"
dotnet run --project Products.API --no-build --no-launch-profile

pause
