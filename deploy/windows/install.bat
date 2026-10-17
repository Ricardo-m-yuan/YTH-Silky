@echo off
chcp 65001 >nul
REM ================================================================
REM YTH-Silky API 管理平台 - Windows 一键部署脚本
REM 前置：.NET 8 SDK、SQL Server 2016+、Consul、Redis、RabbitMQ
REM ================================================================

echo ================================================================
echo   YTH-Silky API 管理平台 - 部署脚本
echo ================================================================
echo.

REM ---- 1. 配置环境变量（按实际环境修改）----
set CONSUL_ADDR=http://127.0.0.1:8500
set REDIS_CONFIG=127.0.0.1:6379,defaultDatabase=0
set RABBIT_HOST=127.0.0.1
set RABBIT_PORT=5672
set RABBIT_USER=guest
set RABBIT_PWD=guest
set SQL_SERVER=.
set SQL_USER=sa
set SQL_PWD=YourPassword123

echo [1/6] 检查 .NET 8 SDK...
dotnet --version 2>nul | findstr "8\." >nul
if errorlevel 1 (
    echo   [错误] 未检测到 .NET 8 SDK，请先安装 https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo   .NET 8 SDK OK

echo.
echo [2/6] 还原 NuGet 包...
dotnet restore YTH-Silky.sln
if errorlevel 1 (
    echo   [错误] NuGet 还原失败，请检查网络或 NuGet 源
    pause
    exit /b 1
)

echo.
echo [3/6] 编译整个解决方案...
dotnet build YTH-Silky.sln -c Release --no-restore
if errorlevel 1 (
    echo   [错误] 编译失败
    pause
    exit /b 1
)
echo   编译成功

echo.
echo [4/6] 提示：请先手动执行以下操作
echo   a) 在 SSMS 执行 database\01_InitDatabases.sql 建库
echo   b) 在 SSMS 执行 database\02_QuartzTables.sql 建 Quartz 表
echo   c) 确保 Consul / Redis / RabbitMQ 已启动
echo   d) 修改各服务的 appsettings.yaml 中的连接信息
echo.
pause

echo.
echo [5/6] 安装为 Windows 服务（需以管理员身份运行）...
echo   正在安装 Identity 服务...
sc create SilkyPlatform.Identity binPath= "%~dp0src\identity\Silky.Platform.Identity.Host\bin\Release\net8.0\Silky.Platform.Identity.Host.exe" start= auto
sc description SilkyPlatform.Identity "YTH-Silky 身份中心服务"

echo   正在安装 Api 服务...
sc create SilkyPlatform.Api binPath= "%~dp0src\apimanager\Silky.Platform.Api.Host\bin\Release\net8.0\Silky.Platform.Api.Host.exe" start= auto
sc description SilkyPlatform.Api "YTH-Silky 接口管理服务"

echo   正在安装 Job 服务...
sc create SilkyPlatform.Job binPath= "%~dp0src\schedule\Silky.Platform.Job.Host\bin\Release\net8.0\Silky.Platform.Job.Host.exe" start= auto
sc description SilkyPlatform.Job "YTH-Silky 定时任务服务"

echo   正在安装 Log 服务...
sc create SilkyPlatform.Log binPath= "%~dp0src\log\Silky.Platform.Log.Host\bin\Release\net8.0\Silky.Platform.Log.Host.exe" start= auto
sc description SilkyPlatform.Log "YTH-Silky 日志服务"

echo   正在安装 Gateway 服务...
sc create SilkyPlatform.Gateway binPath= "%~dp0src\gateway\Silky.Platform.Gateway\bin\Release\net8.0\Silky.Platform.Gateway.exe" start= auto
sc description SilkyPlatform.Gateway "YTH-Silky 网关服务"

echo.
echo [6/6] 启动服务...
net start SilkyPlatform.Identity
net start SilkyPlatform.Api
net start SilkyPlatform.Job
net start SilkyPlatform.Log
net start SilkyPlatform.Gateway

echo.
echo ================================================================
echo   部署完成！
echo   - 网关地址: http://localhost:5000
echo   - Swagger 文档: http://localhost:5000/swagger
echo   - Dashboard: http://localhost:5000/dashboard
echo   - Consul UI: http://localhost:8500
echo ================================================================
pause
