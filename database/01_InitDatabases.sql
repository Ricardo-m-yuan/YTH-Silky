/* ================================================================
 * YTH-Silky API 管理平台 - 数据库初始化脚本
 * 目标：SQL Server 2016+
 * 说明：创建 4 个业务库，各服务通过 EF Core Migration 建表
 *       本脚本只建库 + 预置种子数据（管理员账号/基础权限）
 * 使用：在 SSMS 中以 sysadmin 身份执行
 * ================================================================ */

-- 防止因已存在的库报错
IF DB_ID('Platform_Identity') IS NULL
    CREATE DATABASE Platform_Identity;
IF DB_ID('Platform_Api') IS NULL
    CREATE DATABASE Platform_Api;
IF DB_ID('Platform_Schedule') IS NULL
    CREATE DATABASE Platform_Schedule;
IF DB_ID('Platform_Log') IS NULL
    CREATE DATABASE Platform_Log;

GO
PRINT '=== 4 个业务库创建完成 ===';

-- ================================================================
-- Platform_Identity 种子数据
-- ================================================================
USE [Platform_Identity];
GO

-- 注意：EF Core 首次运行会通过 Migration 建表；
-- 如果未启用 Migration（首次启动 EnsureCreated），以下种子数据可在建表后手动执行。
-- 以下脚本假设 Identity 表已由 EF Core 创建。

/* 超级管理员账号：admin / Admin@123（密码哈希为 SHA256 + 盐） */
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE UserName = 'admin')
BEGIN
    INSERT INTO [Users] (UserName, PasswordHash, Salt, RealName, Email, Phone, Status, CreatedTime, IsDeleted)
    VALUES (
        'admin',
        -- 明文: Admin@123  盐: silky-platform-salt
        CONVERT(VARBINARY(MAX), HASHBYTES('SHA2_256', 'Admin@123' + 'silky-platform-salt')),
        'silky-platform-salt',
        '超级管理员',
        'admin@yth-silky.com',
        '13800000000',
        1,
        GETDATE(),
        0
    );
    PRINT '==> 种子用户 admin 创建完成';
END
GO

/* 系统管理员角色 */
USE [Platform_Identity];
GO
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE Name = 'SuperAdmin')
BEGIN
    INSERT INTO [Roles] (Name, Code, Description, Status, CreatedTime, IsDeleted)
    VALUES ('超级管理员', 'SuperAdmin', '拥有全部权限', 1, GETDATE(), 0);
    PRINT '==> 种子角色 SuperAdmin 创建完成';
END
GO

-- ================================================================
-- 权限种子数据（接口管理用，初始化在 Identity 库的 Permissions 表）
-- ================================================================
USE [Platform_Identity];
GO
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE Code = '*')
BEGIN
    INSERT INTO [Permissions] (Code, Name, Type, [Group], Sort, Remark, CreatedTime)
    VALUES ('*', '全部权限', 3, 'system', 0, '超级权限码，匹配一切', GETDATE());
    PRINT '==> 种子权限 * 创建完成';
END
GO

PRINT '=== YTH-Silky 数据库初始化完成 ===';
PRINT '注意：业务表请通过 EF Core Migration 自动创建';
PRINT '命令: dotnet ef database update --project <Application> --startup-project <Host>';
GO
