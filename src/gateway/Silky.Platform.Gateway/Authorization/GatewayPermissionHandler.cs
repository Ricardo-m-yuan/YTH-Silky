using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silky.Http.Identity.Authorization.Handlers;
using Silky.Http.Identity.Authorization.Requirements;
using Silky.Platform.Identity.Application.Contracts.Permissions;
using System.Security.Claims;

namespace Silky.Platform.Gateway.Authorization;

/// <summary>
/// 网关权限校验器：从 JWT Claims 取 userId，通过 RPC 调 Identity.CheckUserPermissionAsync。
/// 遇到 PermissionRequirement 则校验；无 requirement 的匿名策略直接放行。
/// </summary>
public class GatewayPermissionHandler : SilkyAuthorizationHandlerBase
{
    private readonly ILogger<GatewayPermissionHandler> _logger;

    public GatewayPermissionHandler(ILogger<GatewayPermissionHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<bool> PolicyPipelineAsync(
        AuthorizationHandlerContext context,
        HttpContext httpContext,
        IAuthorizationRequirement requirement)
    {
        if (requirement is PermissionRequirement pr)
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User.FindFirst("sub")?.Value;
            if (!long.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("Permission check failed: no valid userId in token");
                return false;
            }

            // 通过 Silky RPC 调用 Identity 服务的权限校验接口
            var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionAppService>();
            var ok = await permissionService.CheckUserPermissionAsync(userId, pr.PermissionName);
            if (!ok)
            {
                _logger.LogInformation("Permission denied: userId={UserId} code={Code}", userId, pr.PermissionName);
            }
            return ok;
        }

        return true;
    }
}
