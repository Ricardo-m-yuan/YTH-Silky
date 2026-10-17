using Silky.Platform.Core.Dtos;

namespace Silky.Platform.Identity.Application.Contracts.Users;

public class UserDto
{
    public long Id { get; set; }

    public string UserName { get; set; }

    public string RealName { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    /// <summary>状态：0 禁用，1 启用</summary>
    public int Status { get; set; }

    public string Remark { get; set; }

    public DateTime CreatedTime { get; set; }

    public IReadOnlyList<string> RoleCodes { get; set; } = new List<string>();
}

public class GetUserPageInput : PagedQueryInput
{
    public int? Status { get; set; }
}

public class CreateUserInput
{
    public string UserName { get; set; }

    public string RealName { get; set; }

    public string Password { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public string Remark { get; set; }

    public IReadOnlyList<long> RoleIds { get; set; } = new List<long>();
}

public class UpdateUserInput
{
    public long Id { get; set; }

    public string RealName { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public string Remark { get; set; }

    public IReadOnlyList<long> RoleIds { get; set; } = new List<long>();
}

public class ResetPasswordInput
{
    public long Id { get; set; }

    public string NewPassword { get; set; }
}

public class SetUserRolesInput
{
    public long UserId { get; set; }

    public IReadOnlyList<long> RoleIds { get; set; } = new List<long>();
}

public class SetUserStatusInput
{
    public long UserId { get; set; }

    /// <summary>0 禁用，1 启用</summary>
    public int Status { get; set; }
}
