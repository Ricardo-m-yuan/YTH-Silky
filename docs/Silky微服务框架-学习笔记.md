# Silky 微服务框架 · 深度学习笔记

> 基于官方文档 + GitHub 源码（v3.x，支持 .NET 6/7/8/9/10）整理。
> 仓库：`https://github.com/liuhll/silky` ｜ 文档：`https://docs.silky-fk.com/silky/`

---

## 一、它是什么

Silky 是 .NET 平台下的**开源微服务开发框架**，核心只做两件事：**RPC 通信** 与 **微服务治理**。

一句话理解它的设计哲学：

> **接口即服务** —— 一个标注了 `[ServiceRoute]` 的普通 C# 接口，就是一个完整的微服务端点。框架自动生成 HTTP WebAPI 和 RPC 端点，不需要手写 Controller，不需要配置路由表。

它把"普通的 .NET 主机应用"以最小侵入的方式升级为微服务应用。对比 Dubbo / Spring Cloud / Dapr，Silky 的最大差异是：**对 .NET 开发者零额外心智负担**——不需要 Sidecar 进程，不需要学 Protobuf 代码生成，不需要新的编程范式，你会接口、DI、中间件就够了。

| 特性 | Silky | Dapr | gRPC |
|---|---|---|---|
| 开发语言 | C#/.NET | 多语言 | 多语言 |
| RPC 方式 | **接口代理（透明调用）** | HTTP/gRPC Sidecar | Protobuf 代码生成 |
| 服务治理 | 内置 | 内置（Sidecar） | 需自行实现 |
| 分布式事务 | TCC（内置） | Saga | 不支持 |
| 部署复杂度 | 低 | 高（需 Runtime） | 低 |

---

## 二、整体架构

```
┌──────────────────────────────────────────────────────────┐
│               HTTP / WebSocket 客户端                     │
└─────────────────────────┬────────────────────────────────┘
                          │ HTTPS
            ┌─────────────▼─────────────┐
            │       Silky API 网关       │
            │  Swagger · JWT · 限流      │
            │  CORS · 审计 · Dashboard   │
            └─────────────┬─────────────┘
                          │ DotNetty TCP 长连接 (RPC)
       ┌──────────────────┼──────────────────┐
       │                  │                  │
 ┌─────▼─────┐      ┌─────▼─────┐     ┌─────▼─────┐
 │  服务 A   │ ←──► │  服务 B   │ ←──►│  服务 C   │
 │  (Host)   │  RPC │  (Host)   │  RPC│  (Host)   │
 └─────┬─────┘      └─────┬─────┘     └─────┬─────┘
       └──────────────────┼──────────────────┘
                          │
         ┌────────────────┼────────────────┐
   ┌─────▼──────┐  ┌──────▼─────┐  ┌──────▼──────┐
   │ Consul /   │  │  Redis /   │  │ MySQL / PG  │
   │ Nacos / ZK │  │ 分布式缓存 │  │ (EF Core)   │
   └────────────┘  └────────────┘  └─────────────┘
```

分层职责：

| 层 | 职责 |
|---|---|
| **服务引擎 + 模块化系统** | 主机初始化、模块解析/依赖排序/加载、服务注册与解析 |
| **RPC 通信层** | DotNetty + TCP 长连接、Autofac 动态代理、JSON/MessagePack/ProtoBuf 编解码、心跳、RPC Token |
| **微服务治理层** | 注册发现、负载均衡、超时、故障转移、熔断（Polly）、限流、降级、缓存拦截 |
| **主机类型** | 通用主机 / Web 主机 / WebSocket 主机 / 网关主机 |
| **可观测性** | SkyAPM 链路追踪、Serilog 结构化日志、Dashboard 控制台 |

---

## 三、核心概念

### 3.1 四种主机类型

这是 Silky 最基础的选型决策，决定了应用对外暴露什么能力：

| 主机类型 | 构建方法 | 对外 HTTP | RPC 服务 | WebSocket | 典型场景 |
|---|---|---|---|---|---|
| **通用主机** | `ConfigureSilkyGeneralHostDefaults()` | ❌ | ✅ | ❌ | 纯内部业务服务（最常见） |
| **Web 主机** | `ConfigureSilkyWebHostDefaults(...)` | ✅ | ✅ | ❌ | 需要自带 HTTP 端口的服务 |
| **WebSocket 主机** | `ConfigureSilkyWebSocketDefaults()` | ❌ | ✅ | ✅ | 需要服务端主动推送 |
| **网关主机** | `ConfigureSilkyGatewayDefaults(...)` | ✅ | ❌ | ❌ | 集群统一入口 |

每个方法都有泛型版本（如 `ConfigureSilkyGeneralHost<T>()`），允许你传入自定义启动模块。

### 3.2 模块化系统（借鉴 ABP）

Silky 的每个 NuGet 包基本都是一个模块。模块定义派生自 `SilkyModule`（它同时是 `Autofac.Module`）：

```csharp
public class MyModule : SilkyModule
{
    // 1. 通过 IServiceCollection 注册服务
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

    // 2. 通过 Autofac ContainerBuilder 注册服务
    protected override void RegisterServices(ContainerBuilder builder) { }

    // 3. 应用启动时执行初始化（如数据库迁移）
    public override Task Initialize(ApplicationContext applicationContext) => Task.CompletedTask;

    // 4. 应用停止时释放资源
    public override Task Shutdown(ApplicationContext applicationContext) => Task.CompletedTask;
}
```

要点：

- 用 `[DependsOn(typeof(OtherModule))]` 声明依赖，框架**自动拓扑排序**，无需手工管理注册顺序
- 需要 HTTP 中间件的模块派生自 `HttpSilkyModule`，多一个 `Configure(IApplicationBuilder app)` 方法
- **坑点**：用了 `HttpSilkyModule` 的话，`Startup.Configure` 里必须调用 `app.ConfigureSilkyRequestPipeline();`，否则中间件不生效
- 启动时的模块初始化顺序：`RpcModule` → `DotNettyTcpModule` / `TransactionModule` / `WebSocketModule` → 启动模块
- 支持插件化加载模块和应用服务（配置 `plugInSource`）

### 3.3 服务条目（ServiceEntry）

**服务条目是 Silky 的路由最小单元**——接口的每个方法生成一个或多个 ServiceEntry，注册中心同步的元数据就是它。框架的路由、治理、限流、缓存拦截全部以服务条目为粒度。

启动时主机完成的关键工作：
1. 订阅 RPC 消息监听
2. 解析应用服务 → 生成服务条目
3. 启动 RPC 消息监听
4. 解析本机端点并向注册中心注册
5. 从注册中心拉取全量服务元数据与其他实例
6. 心跳维持，持续同步元数据

---

## 四、5 分钟上手

### 通用主机（最小可运行）

`Program.cs`：

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureSilkyGeneralHostDefaults()
            .Build()
            .RunAsync();
    }
}
```

服务契约（单独放 `Application.Contracts` 项目，供其他服务引用）：

```csharp
[ServiceRoute]
public interface IGreetingAppService
{
    Task<string> Say(string line);
}
```

服务实现：

```csharp
public class GreetingAppService : IGreetingAppService
{
    public Task<string> Say(string line) => Task.FromResult($"Hello {line}");
}
```

**就这样。** 一个可被 RPC 调用、可被网关暴露成 `POST /api/greeting/say` 的微服务就完成了。只需要引用 `Silky.Agent.Host` 包。

### 网关

```csharp
await Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGatewayDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
    .Build().RunAsync();
```

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilkyHttpCore()
            .AddSwaggerDocuments()
            .AddRouting()
            .AddDashboard();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwaggerDocuments();
        }
        app.UseDashboard();
        app.UseRouting();
        app.UseWebSockets();
        app.UseSilkyWebSocketsProxy();
        app.UseSilkyHttpServer();
        app.UseEndpoints(endpoints => { endpoints.MapSilkyRpcServices(); });
    }
}
```

### 官方脚手架

用模板一键生成整套解决方案（含 Docker Compose、Apollo、SkyWalking）：

```bash
dotnet new --install Silky.App.Template
dotnet new silky.app -n MyApp
```

---

## 五、接口即服务：路由是怎么生成的（重点）

这是日常写业务最常碰到、也最容易踩坑的部分。

### 5.1 ServiceRoute 特性

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public class ServiceRouteAttribute : Attribute, IRouteTemplateProvider
{
    public ServiceRouteAttribute() : this("api/{appservice}") { }   // 默认模板
    public ServiceRouteAttribute(string template) { Template = template; }
    public string Template { get; }
    public string ServiceName { get; set; }
}
```

- **默认路由模板**：`api/{appservice}`
- 可自定义：`[ServiceRoute(template: "api/system/{appservice=silkyapp}")]`
- 可显式指定服务名：`[ServiceRoute(ServiceName = "order")]`

### 5.2 服务名推导

```
IAccountAppService
  → 去掉 "I" 前缀          →  AccountAppService
  → 移除 "AppService"      →  Account      ← 最终服务名
```

其中 `AppService` 来自 `SilkyApplicationCreationOptions.ServiceTemplateName`（默认值）。所以默认路由下 `IAccountAppService` 的服务名就是 `account`，路由前缀 `api/account`。

### 5.3 HTTP 方法推断规则

按以下优先级判定：

1. **方法上标注了 `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpDelete]`** → 直接采用
2. **未标注** → 看参数：
   - 所有参数都是简单类型或可空类型 → `GET`
   - 否则（有复杂对象入参）→ `POST`
3. **再按方法名前缀修正**（仅对未标注的情况生效）：

| 方法名前缀 | 修正为 |
|---|---|
| `Create` | POST |
| `Update` | PUT |
| `Delete` | DELETE |
| `Search` / `Query` / `Get` | GET |

### 5.4 RESTful 路由模板生成

`Governance:ApiIsRESTfulStyle` 默认 `true`。开启后会做两件事：

1. 去掉方法名的 `Async` 后缀
2. 去掉方法名的 HTTP 语义前缀：

| HTTP 方法 | 会被剥离的前缀 |
|---|---|
| GET | `GetBy` / `GetFor` / `GetFrom` / `Get` |
| POST | `CreateOrUpdate` / `CreateOrModify` / `Create` / `Add` |
| PUT / PATCH | `CreateOrUpdate` / `CreateOrModify` / `Update` / `Put` / `Modify` |
| DELETE | `Delete` |

最后把 Path 参数追加到路由尾部。

> **重要**：路由生成完成后，`Router.GenerateRoutePath()` 会对整条路由执行 `ToLower()`，所以**最终路由全为小写**。例如 `IGreetingAppService.Say` 生成的实际路由是 `GET api/greeting/say`，而非 `api/greeting/Say`。

### 5.5 对照表（看一遍就懂）

以 `[ServiceRoute]`（默认 `api/{appservice}`）+ RESTful 为例：

| 接口方法 | 特性 | 生成路由 |
|---|---|---|
| `IAccountAppService.Create(CreateAccountInput)` | 无 | `POST api/account` |
| `IProductAppService.Update(UpdateProductInput)` | 无 | `PUT api/product` |
| `IGreetingAppService.Say(string line)` | 无 | `GET api/greeting/say` |
| `IProductAppService.Get(long id)` | `[HttpGet("{id:long}")]` | `GET api/product/{id:long}` |
| `IAccountAppService.GetAccountByName(string name)` | `[HttpGet("{name}")]` | `GET api/account/{name}` |
| `IAccountAppService.GetLoginUserInfo()` | `[HttpGet("current/userinfo")]` | `GET api/account/current/userinfo` |
| `IProductAppService.Delete(long id)` | `[HttpDelete("{id:long}")]` | `DELETE api/product/{id:long}` |

> **踩坑提示**：`Say(string line)` 因为参数是简单类型被推断为 GET，路由里方法名不带前缀可剥离，所以保留为 `say`。如果你想让它走 POST 且带请求体，请显式加 `[HttpPost]`，并配合 `[ServiceRoute]` 的模板调整。

---

## 六、RPC 调用：像本地方法一样

### 6.1 依赖注入即调用

调用方只需引用契约项目（Application.Contracts），然后注入接口：

```csharp
public class OrderAppService : IOrderAppService
{
    private readonly IGreetingAppService _greetingService;
    private readonly IProductAppService _productService;

    public OrderAppService(IGreetingAppService greetingService, IProductAppService productService)
    {
        _greetingService = greetingService;
        _productService = productService;
    }

    public async Task<string> CreateAsync(string customerName)
    {
        // 底层：Autofac 动态代理 → 服务发现 → 负载均衡 → DotNetty RPC
        return await _greetingService.Say(customerName);
    }
}
```

代理内部串起来的完整链路：**缓存拦截 → 熔断 → 限流 → 负载均衡 → RPC 调用 → 故障转移重试 → 降级**。这些全是自动的。

### 6.2 RpcContext（隐式传参）

基于 `AsyncLocal` 的 RPC 上下文，可在服务间透传数据，不用污染方法签名：

```csharp
// 生产者
RpcContext.Context.SetInvokeAttachment("orderId", order.Id.ToString());

// 消费者
var orderId = RpcContext.Context.GetInvokeAttachment("orderId");
```

它还提供 `RpcContext.Context.Connection`（客户端/服务端 Host、Port、协议）等信息，在 TCC 事务里经常用到。

---

## 七、服务治理

### 7.1 全局配置（`Governance` 节点）

| 属性 | 说明 | 默认值 |
|---|---|---|
| `ShuntStrategy` | 负载均衡：`Polling` / `Random` / `HashAlgorithm` | `Polling` |
| `TimeoutMillSeconds` | RPC 超时（ms），0 = 不超时（适合调试） | `5000` |
| `EnableCachingInterceptor` | 缓存拦截总开关 | `true` |
| `EnableCircuitBreaker` | 熔断开关 | `true` |
| `ExceptionsAllowedBeforeBreaking` | 触发熔断的连续异常次数 | `3` |
| `BreakerSeconds` | 熔断持续时长（s） | `60` |
| `RetryTimes` / `RetryIntervalMillSeconds` | 故障转移重试次数 / 间隔 | `3` / `50` |
| `MaxConcurrentHandlingCount` | 单实例最大并发 | `50` |
| `ApiIsRESTfulStyle` | RESTful 路由风格 | `true` |

> 业务异常（友好异常）**不会**触发熔断，只有非业务异常（IO、通信等）才会。

### 7.2 方法级覆盖：`[Governance]`

```csharp
[Governance(ProhibitExtranet = true)]
[Governance(ShuntStrategy = ShuntStrategy.HashAlgorithm, TimeoutMillSeconds = 10000)]
Task<GetProductOutput> DeductStock(DeductStockInput input);
```

`ProhibitExtranet = true` 非常实用：**禁止外部网络直接访问**，只允许集群内部 RPC 调用。像"扣减库存""扣减余额"这类内部方法都应该加上。

### 7.3 服务降级

```csharp
[Fallback(typeof(IProductAppService), MethodName = "GetFallback")]
Task<GetProductOutput> Get(long id);
```

RPC 失败时自动执行降级方法。可标注在方法或接口上。

### 7.4 缓存拦截（性能杀手锏）

RPC 调用时命中缓存直接返回，**根本不发网络请求**。三个特性：

```csharp
// 读：命中直接返回
[GetCachingIntercept("Account:Id:{id}", OnlyCurrentUserData = true)]
Task<GetAccountOutput> GetAccountById(long id);

// 写：更新缓存
[UpdateCachingIntercept("Account:Id:{Id}")]
Task<GetAccountOutput> Update(UpdateAccountInput input);

// 删：失效缓存（需指定 cacheName）
[RemoveCachingIntercept("GetAccountOutput", "Account:Id:{id}")]
Task Delete(long id);
```

细节：
- Key 模板支持 `{参数名}` 占位，也支持 `{0}` 序号
- `OnlyCurrentUserData`：按当前用户隔离缓存（避免用户数据串号）
- `IgnoreMultiTenancy`：忽略多租户维度
- 需要 Redis 后端时引入 `Silky.Caching.StackExchangeRedis` 并把 `DistributedCache:Redis:IsEnabled` 设为 `true`
- `DistributedCache:GlobalCacheEntryOptions:SlidingExpiration` 默认 20 分钟

---

## 八、分布式事务（TCC）

Silky 内置 TCC（Try-Confirm-Cancel），通过 AOP 拦截器 + Redis 事务日志保证最终一致性。

**用法：接口标注 `[Transaction]`，实现标注 `[TccTransaction]`。**

```csharp
// 契约层
[ServiceRoute]
public interface IOrderAppService
{
    [Transaction]
    Task<GetOrderOutput> Create(CreateOrderInput input);
}
```

```csharp
// 实现层
public class OrderAppService : IOrderAppService
{
    [TccTransaction(ConfirmMethod = "OrderCreateConfirm", CancelMethod = "OrderCreateCancel")]
    [UnitOfWork]
    public async Task<GetOrderOutput> Create(CreateOrderInput input)
        => await _orderDomainService.Create(input);

    [UnitOfWork]
    public async Task<GetOrderOutput> OrderCreateConfirm(CreateOrderInput input)
    {
        var orderId = RpcContext.Context.GetInvokeAttachment("orderId");
        var order = await _orderDomainService.GetById(orderId.To<long>());
        order.Status = OrderStatus.Payed;      // Confirm：推进到终态
        return (await _orderDomainService.Update(order)).Adapt<GetOrderOutput>();
    }

    [UnitOfWork]
    public async Task OrderCreateCancel(CreateOrderInput input)
    {
        var orderId = RpcContext.Context.GetInvokeAttachment("orderId");
        if (orderId != null)
        {
            var order = await _orderDomainService.GetById(orderId.To<long>());
            order.Status = OrderStatus.UnPay;  // Cancel：回滚
            await _orderDomainService.Update(order);
        }
    }
}
```

规则与要点：
- Try 阶段通过 `RpcContext` 透传业务 ID 给 Confirm/Cancel（示例中是 `orderId`）
- Confirm/Cancel 必须**幂等**——网络抖动会重试
- 记得处理**空回滚**（Try 未执行就收到 Cancel，示例中 `orderId != null` 判断就是）
- **防悬挂**：Cancel 先于 Try 到达的情况
- 定时任务会自动恢复悬挂事务（`ScheduledRecoveryDelay` 默认 30s，重试上限 `RetryMax` 10 次）
- Confirm/Cancel 方法必须和 Try 方法**参数签名一致**

---

## 九、安全体系

| 机制 | 说明 |
|---|---|
| **网关统一认证** | JWT 在网关层校验，业务服务不用重复实现 |
| **RPC Token** | 集群内共享 `rpc:token`，外部无法绕过网关直接打 RPC 端口（`token` **必填，不允许为空**） |
| **接口级鉴权** | `[Authorize]`（接口级）、`[AllowAnonymous]`（方法级）、`[Governance(ProhibitExtranet = true)]` |
| **SSL** | RPC 通信支持 SSL/TLS（`Rpc:IsSsl` + 证书配置） |

JWT 配置要点：**网关的 `Gateway:JwtSecret` 必须与签发服务的 `JwtSettings:Secret` 完全一致**，否则网关解析不了 Token。

---

## 十、配置速查

配置优先级（低 → 高）：`appsettings.yml` → `appsettings.{Env}.yml` → 环境变量 → 命令行参数。支持 JSON / YAML / Apollo / 环境变量。

### RPC（`Rpc`）

| 属性 | 说明 | 默认 |
|---|---|---|
| `Token` | **必填**，RPC 通信密钥，集群内必须一致 | — |
| `Port` | RPC 监听端口 | `2200` |
| `Host` | 监听 IP，`0.0.0.0` 自动获取 | `0.0.0.0` |
| `Cidr` | 多网卡时用 CIDR 指定网段 | — |
| `IsSsl` / `SslCertificateName` | SSL 配置 | `false` |
| `ConnectTimeout` | 连接超时（ms） | `300` |
| `EnableHeartbeat` | 心跳保活 | `true` |
| `HeartbeatWatchIntervalSeconds` | 心跳间隔，最小 60 | `300` |
| `UseTransportClientPool` / `TransportClientPoolNumber` | 连接池 | `true` / `50` |

### 注册中心（`RegistryCenter`）

三种通过 `Type` 切换，**只需换 NuGet 包 + 改一行配置**：

```yaml
# Consul（需 Silky.RegistryCenter.Consul）
registrycenter:
  type: Consul
  address: http://127.0.0.1:8500

# Zookeeper（需 Silky.RegistryCenter.Zookeeper）
registrycenter:
  type: Zookeeper
  connectionStrings: "127.0.0.1:2181"   # 多节点逗号分隔；多集群分号分隔

# Nacos（需 Silky.RegistryCenter.Nacos）
registrycenter:
  type: Nacos
  serverAddresses:
    - "http://127.0.0.1:8848"
  userName: nacos
  password: nacos
  serverGroupName: SILKYGROUP
```

### 其他关键节点

| 节点 | 用途 | 要点 |
|---|---|---|
| `DistributedCache` | 缓存 | `Redis.IsEnabled=true` 启用 Redis |
| `JwtSettings` | JWT 签发 | `Secret` 必填，与网关 `Gateway:JwtSecret` 一致 |
| `Gateway` | 网关 | `JwtSecret`、`IgnoreWrapperPathPatterns` |
| `CorsAccessor` | 跨域 | `WithOrigins` 空数组 = 允许所有 |
| `Auditing` | 审计日志 | 默认不记录 GET 请求 |
| `WebSocket` | WS | 默认端口 `3000` |
| `SwaggerDocument` | API 文档 | `OrganizationMode: AllAndGroup` 聚合全集群 |
| `Dashboard` | 管理控制台 | `UseAuth`、`StatsPollingInterval` |
| `DistributedTransaction` | TCC 事务 | `TransactionType: Tcc` |
| `RateLimiting` | HTTP 限流 | 基于 AspNetCoreRateLimit |

---

## 十一、生态集成与包清单

Silky 完全兼容 ASP.NET Core 生态，按需引入：

| 类别 | 包 |
|---|---|
| **核心** | `Silky.Agent.Host`、`Silky.Core`、`Silky.Rpc` |
| **ORM** | `Silky.EntityFrameworkCore` / FreeSql / Dapper |
| **注册中心** | `Silky.RegistryCenter.{Zookeeper,Nacos,Consul}` |
| **缓存** | `Silky.Caching`、`Silky.Caching.StackExchangeRedis` |
| **对象映射** | `Silky.ObjectMapper.AutoMapper` / `Silky.ObjectMapper.Mapster` |
| **参数校验** | `Silky.Validation`（DataAnnotations）/ `Silky.Validation.Fluent` |
| **事务** | `Silky.Transaction.Tcc`、`Silky.Transaction.Repository.Redis` |
| **消息总线** | `Silky.MassTransit`、CAP |
| **HTTP 增强** | `Silky.Http.{Core,Swagger,Dashboard,Identity,CorsAccessor,RateLimit,Auditing,MiniProfiler}` |
| **链路追踪** | `Silky.SkyApm.Agent` |
| **日志** | `Silky.Logging.Serilog` |
| **分布式锁** | `Silky.DistributedLock.Redis` |
| **健康检查** | `Silky.HealthChecks.Rpc` |

---

## 十二、推荐的项目分层

官方 `samples/microservices` 采用的是 DDD 分层，值得照抄：

```
Silky.Order.Application.Contracts   ← 契约层：接口 + DTO（被其他服务引用）
  └── Orders/IOrderAppService.cs
Silky.Order.Application             ← 实现层：应用服务
  └── Orders/OrderAppService.cs
Silky.Order.Domain.Shared           ← 领域共享：枚举、常量
Silky.Order.Domain                  ← 领域层：实体、领域服务
Silky.Order.EntityFrameworkCore     ← 数据访问
Silky.Order.Database.Migrations     ← 迁移
Silky.OrderHost                     ← 主机（Program.cs）
```

**黄金法则**：**契约层（Application.Contracts）必须独立成项目**。调用方只引用契约，不引用实现——这是 Silky 透明 RPC 能成立的前提，也是避免服务间隐式耦合的关键。

---

## 十三、学习路径建议

1. **跑通 quickstart** —— `samples/quickstart` 下有 4 个独立最小工程（generalhost / webhost / websocket / gateway），先跑通用主机 + 网关，感受"零配置生成 API"
2. **读 samples/microservices** —— order/account/stock 三个服务构成完整 TCC 事务链路，是理解分布式事务的最佳教材
3. **用官方模板起项目** —— `Silky.App.Template` 生成的是生产级骨架
4. **参考 silky.hero** —— 作者写的完整权限管理系统（多租户 + RBAC），在线 Demo：`https://hero.silky-fk.com`

## 十四、容易踩的坑

| 坑 | 说明 |
|---|---|
| RPC Token 必填 | `rpc:token` 不允许为空，且集群内所有服务必须一致 |
| 网关 JWT 密钥 | `Gateway:JwtSecret` 必须等于 `JwtSettings:Secret` |
| HTTP 模块中间件 | 用了 `HttpSilkyModule` 必须调 `app.ConfigureSilkyRequestPipeline()` |
| 路由不符合预期 | 回看 §5.3/§5.4 的方法名前缀剥离规则，或直接显式标注 `[HttpGet]`/`[HttpPost]` |
| 内部方法暴露到外网 | 记得加 `[Governance(ProhibitExtranet = true)]` |
| 调试时超时 | 开发环境把 `Governance:TimeoutMillSeconds` 设为 `0`（不超时），别设成 0 以外的极小值 |
| TCC 幂等 | Confirm/Cancel 必须幂等，必须处理空回滚与悬挂 |
| 缓存串号 | 用户维度数据加 `OnlyCurrentUserData = true` |

---

## 附：关键 API 速查

```csharp
// —— 路由 ——
[ServiceRoute]                                    // 默认模板 api/{appservice}
[ServiceRoute(template: "api/order/{appservice}")]
[ServiceRoute(ServiceName = "order")]

// —— HTTP 方法 ——
[HttpGet("{id:long}")] [HttpPost] [HttpPut] [HttpDelete("{id}")]

// —— 鉴权 ——
[Authorize]                                       // 接口级
[AllowAnonymous]                                  // 方法级

// —— 治理 ——
[Governance(ProhibitExtranet = true)]
[Governance(ShuntStrategy = ShuntStrategy.HashAlgorithm, TimeoutMillSeconds = 10000)]
[Fallback(typeof(IXxxAppService), MethodName = "Fallback")]

// —— 缓存拦截 ——
[GetCachingIntercept("key:{id}", OnlyCurrentUserData = true)]
[UpdateCachingIntercept("key:{Id}")]
[RemoveCachingIntercept("CacheName", "key:{id}")]

// —— 事务 ——
[Transaction]                                     // 接口方法
[TccTransaction(ConfirmMethod = "XxxConfirm", CancelMethod = "XxxCancel")]  // 实现方法

// —— 工作单元 ——
[UnitOfWork]

// —— 上下文 ——
RpcContext.Context.SetInvokeAttachment("key", "value");
RpcContext.Context.GetInvokeAttachment("key");
RpcContext.Context.Connection;

// —— 主机构建 ——
.ConfigureSilkyGeneralHostDefaults()
.ConfigureSilkyWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
.ConfigureSilkyWebSocketDefaults()
.ConfigureSilkyGatewayDefaults(webBuilder => webBuilder.UseStartup<Startup>())
```
