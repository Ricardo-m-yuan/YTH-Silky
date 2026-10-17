# YTH-Silky · 基于 Silky 微服务框架的 API 管理平台

> 技术栈：.NET 8 · Silky 3.9.2 · SQL Server 2016 · Consul · Redis · RabbitMQ · EF Core · Quartz.NET · 部署于 Windows

## 平台定位

一个面向企业内部的 **API 全生命周期管理平台**，覆盖接口管理、用户管理、权限管理、定时任务管理、网关、日志管理六大域。

## 架构总览

```
                     ┌──────────────────────────────────────┐
                     │           外部客户端 / 前端           │
                     └──────────────────┬───────────────────┘
                                        │ HTTP / WebSocket
                                 ┌──────▼──────┐
                                 │   Gateway   │  ← JWT 鉴权 + RBAC 权限 + Swagger 聚合
                                 │  网关主机   │     (Silky ConfigureSilkyGatewayDefaults)
                                 └──────┬──────┘
                                        │ Silky RPC（Consul 服务发现）
           ┌───────────┬────────────────┼────────────────┬───────────┐
           ▼           ▼                ▼                ▼           ▼
     ┌──────────┐ ┌──────────┐  ┌──────────────┐  ┌──────────┐  ┌──────────┐
     │ Identity │ │   Api    │  │   Schedule   │  │   Log    │  │          │
     │ 用户/权限 │ │ 接口管理 │  │ Quartz定时任务 │  │ 日志消费 │  │          │
     └────┬─────┘ └────┬─────┘  └──────┬───────┘  └────┬─────┘  └──────────┘
          │             │               │                │
     ┌────▼─────┐  ┌────▼─────┐  ┌──────▼───────┐  ┌────▼─────┐
     │Platform_ │  │Platform_ │  │Platform_     │  │Platform_ │
     │Identity  │  │Api       │  │Schedule      │  │Log       │
     └──────────┘  └──────────┘  └──────────────┘  └──────────┘
```

| 服务 | 职责 | RPC 端口 | 数据库 |
|---|---|---|---|
| **Gateway** | 统一入口、JWT 鉴权、RBAC 权限校验、Swagger 聚合、限流 | — | — |
| **Identity** | 用户/角色/权限/登录/JWT 签发 | 2201 | Platform_Identity |
| **Api** | 接口管理：采集 Silky 服务条目、分组、启停、治理配置 | 2202 | Platform_Api |
| **Schedule** | 定时任务 CRUD、启停、立即执行、执行日志 | 2203 | Platform_Schedule |
| **Log** | 操作/登录/异常日志查询与清理（RabbitMQ 消费落库） | 2204 | Platform_Log |

## 目录结构

```
YTH-Silky/
├── YTH-Silky.sln
├── Directory.Build.props          # 版本集中管理
├── database/
│   ├── 01_InitDatabases.sql        # 4 个业务库 + 种子数据
│   └── 02_QuartzTables.sql        # Quartz 官方 SQL Server 建表
├── deploy/windows/
│   ├── install.bat                 # 一键安装 Windows 服务
│   └── uninstall.bat              # 卸载
└── src/
    ├── shared/
    │   ├── Silky.Platform.Core/           # 实体基类、分页、常量
    │   └── Silky.Platform.Contracts/      # 跨服务事件契约（LogEtos）
    ├── identity/
    │   ├── ...Application.Contracts/       # 用户/角色/权限/登录接口
    │   ├── ...Application/                 # EF Core DbContext、AppService 实现
    │   └── ...Host/                       # Program + ConfigureService + appsettings.yaml
    ├── apimanager/
    │   ├── ...Application.Contracts/       # 接口/分组管理接口
    │   ├── ...Application/                 # 服务条目采集 + 运行时管控
    │   └── ...Host/
    ├── schedule/
    │   ├── ...Application.Contracts/       # 任务管理接口
    │   ├── ...Application/                 # Quartz 调度封装 + 内置 HttpCallJob
    │   └── ...Host/
    ├── log/
    │   ├── ...Application.Contracts/       # 日志查询/清理接口
    │   ├── ...Application/                 # RabbitMQ 消费者 + 落库
    │   └── ...Host/
    └── gateway/
        └── Silky.Platform.Gateway/        # 网关 + JWT + 权限处理器 + Swagger
```

## 核心设计决策

### 1. 不使用 Silky.EntityFrameworkCore，改用原生 EF Core 8

`Silky.EntityFrameworkCore` 是 Furion 风格的隐式 DbContext 配置，对多库分库场景反而碍事。本项目直接 `services.AddDbContext<XXXDbContext>(o => o.UseSqlServer(...))`，清晰可控。

### 2. 定时任务用 Quartz.NET（框架无内置调度）

Silky 没有调度模块。选 Quartz.NET 的理由：
- CRON 表达式、misfire 补偿、集群支持成熟
- 许可证 Apache-2.0 干净
- 任务定义存应用层 `JobInfo` 表，Quartz 调度器在内存中管理 Trigger；服务重启时从数据库重新加载

### 3. RabbitMQ 通过 MassTransit 接入

`Silky.MassTransit` 只带 MassTransit 核心，RabbitMQ transport 要自己加 `MassTransit.RabbitMQ`。日志服务消费 `OperationLogEto` / `LoginLogEto` / `ExceptionLogEto` 三种事件，业务服务发布即解耦。

### 4. 网关权限：自定义 `SilkyAuthorizationHandlerBase`

网关通过 `AddSilkyIdentity<GatewayPermissionHandler>()` 注入自定义权限处理器。JWT 验签由框架 `SilkyAuthenticationHandler` 用 `gateway:jwtSecret` 完成；权限校验通过 RPC 调用 Identity 服务的 `CheckUserPermissionAsync(userId, permissionCode)`。

### 5. 接口管理：利用 `IServiceEntryManager` 采集 + 运行时管控

通过 Silky 的 `IServiceEntryManager` 获取所有微服务暴露的接口元数据（服务名/路由/HTTP方法/参数/治理配置），入库做分组、启停、限流配置。`ServiceEntry.GovernanceOptions` 和 `ServiceEntryDescriptor` 可写，支持运行时动态调整。

## 快速开始

### 前置依赖

| 组件 | 版本 | 说明 |
|---|---|---|
| .NET SDK | 8.0+ | `dotnet --version` 需显示 8.x |
| SQL Server | 2016+ | 4 个业务库 |
| Consul | 1.x+ | `http://127.0.0.1:8500` |
| Redis | 3.0+ | `127.0.0.1:6379` |
| RabbitMQ | 3.8+ | `127.0.0.1:5672` guest/guest |

### 部署步骤

```bash
# 1. 编译
dotnet restore YTH-Silky.sln
dotnet build YTH-Silky.sln -c Release

# 2. 建库（SSMS 执行）
#    database/01_InitDatabases.sql    → 4 个业务库 + 种子管理员
#    database/02_QuartzTables.sql     → Quartz 表（在 Platform_Schedule 库）

# 3. EF Core Migration 建表（各服务 Host 目录下）
dotnet ef migrations add Init --project ../Application --startup-project .
dotnet ef database update --project ../Application --startup-project .

# 4. 启动中间件（Consul / Redis / RabbitMQ）

# 5. 修改各服务 appsettings.yaml 中的连接信息

# 6. Windows 部署（管理员运行）
deploy\windows\install.bat
```

### 开发模式（命令行启动）

```bash
# 各服务可在各自 Host 目录下直接 dotnet run
cd src/identity/Silky.Platform.Identity.Host && dotnet run
cd src/apimanager/Silky.Platform.Api.Host && dotnet run
cd src/schedule/Silky.Platform.Job.Host && dotnet run
cd src/log/Silky.Platform.Log.Host && dotnet run
cd src/gateway/Silky.Platform.Gateway && dotnet run
```

## 关键配置项速查

### JWT 密钥一致性（必须）

网关 `appsettings.yaml` 的 `gateway.jwtSecret` 必须与 Identity 服务的 `jwtSettings.secret` **完全一致**，否则网关验签失败。

| 位置 | 配置节 | 值 |
|---|---|---|
| Gateway | `gateway.jwtSecret` | `6d9a1f4c8b2e7a3d5c0f8e6b4a2d1c9e` |
| Identity | `jwtSettings.secret` | `6d9a1f4c8b2e7a3d5c0f8e6b4a2d1c9e` |

### RPC Token 一致性

所有服务的 `rpc.token` 必须一致，否则 RPC 调用鉴权失败：`ypjdYOzNd4FwENJiEARMLWwK0v7QUHPW`

### 配置节速查

| 配置节 | 作用 | 关键字段 |
|---|---|---|
| `rpc` | RPC 通信 | host / port / token / transportClientPoolNumber |
| `registrycenter` | 服务注册 | type=Consul / address / registerSwaggerDoc / healthCheckIntervalSecond |
| `distributedCache.redis` | 分布式缓存 | isEnabled / configuration |
| `governance` | 服务治理 | timeoutMillSeconds / maxConcurrentHandlingCount / enableCircuitBreaker / apiIsRESTfulStyle |
| `gateway` | 网关专属 | jwtSecret |
| `swaggerDocument` | Swagger 聚合 | organizationMode / injectMiniProfiler / loginInfo |
| `rabbitMq` | RabbitMQ（自读） | host / port / userName / password / virtualHost |
| `connectionStrings` | 数据库连接 | default |
| `jwtSettings` | JWT（Identity） | secret / expiredTime |

## 默认管理员账号

| 字段 | 值 |
|---|---|
| 用户名 | admin |
| 密码 | Admin@123 |
| 角色 | SuperAdmin |
| 权限码 | `*`（匹配一切） |

## 技术栈版本

| 包 | 版本 |
|---|---|
| Silky 全家桶 | 3.9.2 |
| EF Core + SqlServer | 8.0.32 |
| Quartz.NET | 3.22.6 |
| MassTransit + RabbitMQ | 8.5.11 |
| Serilog.AspNetCore | 8.0.3 |
| Microsoft.Extensions.Hosting.WindowsServices | 8.0.1 |

## 许可证

MIT
