using System.Security.Claims;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Silky.Core.Exceptions;
using Silky.Jwt;
using Silky.Jwt.Configuration;
using Silky.Platform.Contracts.Consts;
using Silky.Platform.Contracts.Etos;
using Silky.Platform.Core.Consts;
using Silky.Platform.Identity.Application.Contracts.Accounts;
using Silky.Platform.Identity.Application.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;
using Silky.Platform.Identity.Application.Permissions;
using Silky.Platform.Identity.Application.Security;
using JWT;
using JWT.Exceptions;

namespace Silky.Platform.Identity.Application.Accounts;

/// <summary>
/// 账号服务：登录签发 JWT、刷新令牌、当前用户信息
/// </summary>
public class AccountAppService : IAccountAppService
{
    private readonly IdentityDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IJwtDecoder _jwtDecoder;
    private readonly JwtOptions _jwtOptions;
    private readonly IDistributedCache _cache;
    private readonly UserPermissionResolver _permissionResolver;
    private readonly IPublishEndpoint _publishEndpoint;

    public AccountAppService(
        IdentityDbContext db,
        IJwtTokenGenerator jwtTokenGenerator,
        IJwtDecoder jwtDecoder,
        IOptions<JwtOptions> jwtOptions,
        IDistributedCache cache,
        UserPermissionResolver permissionResolver,
        IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtDecoder = jwtDecoder;
        _jwtOptions = jwtOptions.Value;
        _cache = cache;
        _permissionResolver = permissionResolver;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<LoginOutput> LoginAsync(LoginInput input)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
        {
            throw new BusinessException("用户名或密码不能为空");
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == input.UserName && !u.IsDeleted);

        var ip = CurrentUserAccessor.GetIp();

        if (user == null)
        {
            await PublishLoginLogAsync(null, input.UserName, ip, 0, "用户不存在");
            throw new BusinessException("用户名或密码错误");
        }

        if (user.Status != 1)
        {
            await PublishLoginLogAsync(user.Id, user.UserName, ip, 0, "账号已禁用");
            throw new BusinessException("账号已禁用，请联系管理员");
        }

        if (!PasswordHasher.Verify(input.Password, user.PasswordHash, user.PasswordSalt))
        {
            await PublishLoginLogAsync(user.Id, user.UserName, ip, 0, "密码错误");
            throw new BusinessException("用户名或密码错误");
        }

        var roles = await _permissionResolver.GetRoleCodesAsync(user.Id);
        var permissions = await _permissionResolver.GetPermissionCodesAsync(user.Id);

        await CachePermissionsAsync(user.Id, permissions);

        // 更新最近登录信息
        var tracked = await _db.Users.FirstAsync(u => u.Id == user.Id);
        tracked.LastLoginTime = DateTime.Now;
        tracked.LastLoginIp = ip;
        await _db.SaveChangesAsync();

        await PublishLoginLogAsync(user.Id, user.UserName, ip, 1, "登录成功");

        return new LoginOutput
        {
            Token = GenerateToken(user, roles),
            ExpiredTime = _jwtOptions.ExpiredTime,
            UserId = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Roles = roles
        };
    }

    public async Task<LoginOutput> RefreshAsync(RefreshTokenInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Token))
        {
            throw new BusinessException("令牌不能为空");
        }

        IDictionary<string, object> payload;
        try
        {
            payload = _jwtDecoder.DecodeToObject<IDictionary<string, object>>(
                input.Token, _jwtOptions.Secret, true);
        }
        catch (TokenExpiredException)
        {
            throw new BusinessException("令牌已过期，请重新登录");
        }
        catch (SignatureVerificationException)
        {
            throw new BusinessException("令牌签名非法");
        }

        if (payload == null || !payload.TryGetValue(PlatformConsts.ClaimUserId, out var userIdValue))
        {
            throw new BusinessException("令牌格式非法");
        }

        if (!long.TryParse(userIdValue?.ToString(), out var userId))
        {
            throw new BusinessException("令牌格式非法");
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null || user.Status != 1)
        {
            throw new BusinessException("用户不存在或已禁用");
        }

        var roles = await _permissionResolver.GetRoleCodesAsync(user.Id);
        var permissions = await _permissionResolver.GetPermissionCodesAsync(user.Id);
        await CachePermissionsAsync(user.Id, permissions);

        return new LoginOutput
        {
            Token = GenerateToken(user, roles),
            ExpiredTime = _jwtOptions.ExpiredTime,
            UserId = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Roles = roles
        };
    }

    public async Task<CurrentUserOutput> GetCurrentUserInfoAsync()
    {
        var userId = CurrentUserAccessor.GetUserId();
        if (userId == null)
        {
            throw new BusinessException("未获取到当前登录用户");
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        return new CurrentUserOutput
        {
            UserId = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Roles = await _permissionResolver.GetRoleCodesAsync(user.Id),
            Permissions = await _permissionResolver.GetPermissionCodesAsync(user.Id)
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordInput input)
    {
        var userId = CurrentUserAccessor.GetUserId();
        if (userId == null)
        {
            throw new BusinessException("未获取到当前登录用户");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        if (!PasswordHasher.Verify(input.OldPassword, user.PasswordHash, user.PasswordSalt))
        {
            throw new BusinessException("原密码错误");
        }

        var (hash, salt) = PasswordHasher.Hash(input.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task LogoutAsync()
    {
        var userId = CurrentUserAccessor.GetUserId();
        if (userId == null)
        {
            return;
        }

        await _cache.RemoveAsync(string.Format(PlatformConsts.UserPermissionCacheKey, userId.Value));
    }

    private string GenerateToken(User user, IReadOnlyList<string> roles)
    {
        var payload = new Dictionary<string, object>
        {
            { PlatformConsts.ClaimUserId, user.Id.ToString() },
            { PlatformConsts.ClaimUserName, user.UserName },
            { ClaimTypes.Name, user.UserName },
            { ClaimTypes.NameIdentifier, user.Id.ToString() },
            // 角色必须使用 ClaimTypes.Role 作为键，网关才会拆分并生成角色声明
            { ClaimTypes.Role, string.Join(",", roles) }
        };

        return _jwtTokenGenerator.Generate(payload);
    }

    private async Task CachePermissionsAsync(long userId, IReadOnlyList<string> permissions)
    {
        var key = string.Format(PlatformConsts.UserPermissionCacheKey, userId);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow =
                TimeSpan.FromMinutes(PlatformConsts.UserPermissionCacheMinutes)
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(permissions), options);
    }

    private async Task PublishLoginLogAsync(long? userId, string userName, string ip, int status, string message)
    {
        await _publishEndpoint.Publish(new LoginLogEto
        {
            UserId = userId,
            UserName = userName,
            Ip = ip,
            Status = status,
            Message = message,
            LoginTime = DateTime.Now
        });
    }
}
