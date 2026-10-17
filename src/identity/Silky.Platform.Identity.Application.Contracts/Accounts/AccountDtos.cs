namespace Silky.Platform.Identity.Application.Contracts.Accounts;

public class LoginInput
{
    public string UserName { get; set; }

    public string Password { get; set; }
}

public class RefreshTokenInput
{
    public string Token { get; set; }
}

public class LoginOutput
{
    /// <summary>JWT 令牌</summary>
    public string Token { get; set; }

    /// <summary>过期时间（小时）</summary>
    public int ExpiredTime { get; set; }

    public long UserId { get; set; }

    public string UserName { get; set; }

    public string RealName { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}

public class CurrentUserOutput
{
    public long UserId { get; set; }

    public string UserName { get; set; }

    public string RealName { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = new List<string>();

    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
}

public class ChangePasswordInput
{
    public string OldPassword { get; set; }

    public string NewPassword { get; set; }
}
