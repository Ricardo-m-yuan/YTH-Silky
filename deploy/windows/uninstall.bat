@echo off
chcp 65001 >nul
REM YTH-Silky 卸载脚本 - 停止并删除所有 Windows 服务

echo 正在停止 YTH-Silky 服务...
net stop SilkyPlatform.Gateway 2>nul
net stop SilkyPlatform.Log 2>nul
net stop SilkyPlatform.Job 2>nul
net stop SilkyPlatform.Api 2>nul
net stop SilkyPlatform.Identity 2>nul

echo 正在删除服务...
sc delete SilkyPlatform.Gateway
sc delete SilkyPlatform.Log
sc delete SilkyPlatform.Job
sc delete SilkyPlatform.Api
sc delete SilkyPlatform.Identity

echo 卸载完成
pause
