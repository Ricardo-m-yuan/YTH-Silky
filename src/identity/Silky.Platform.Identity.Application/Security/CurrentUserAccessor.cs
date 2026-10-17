using System.Security.Claims;
using Silky.Core.Runtime.Rpc;

namespace Silky.Platform.Identity.Application.Security;

/// <summary>
/// 当前登录用户读取器。
/// <para>微服务不直接处理 HTTP 请求，用户信息由网关鉴权后写入 RpcContext 附件传递。</para>
/// </summary>
public static class CurrentUserAccessor
{
    public static long? GetUserId()
    {
        var value = RpcContext.Context.GetInvokeAttachment(PlatformClaimNames.UserId)
                    ?? RpcContext.Context.GetInvokeAttachment(ClaimTypes.NameIdentifier);
        return ToLong(value);
    }

    public static string GetUserName()
    {
        var value = RpcContext.Context.GetInvokeAttachment(PlatformClaimNames.UserName)
                    ?? RpcContext.Context.GetInvokeAttachment(ClaimTypes.Name);
        return value?.ToString();
    }

    public static string GetIp()
    {
        var value = RpcContext.Context.GetInvokeAttachment(PlatformClaimNames.Ip);
        return value?.ToString();
    }

    private static long? ToLong(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is long l)
        {
            return l;
        }

        return long.TryParse(value.ToString(), out var result) ? result : null;
    }
}

/// <summary>
/// RpcContext 附件键名，需与网关写入的键保持一致
/// </summary>
public static class PlatformClaimNames
{
    public const string UserId = "userid";

    public const string UserName = "username";

    public const string Ip = "ip";

    public const string Roles = "roles";
}
