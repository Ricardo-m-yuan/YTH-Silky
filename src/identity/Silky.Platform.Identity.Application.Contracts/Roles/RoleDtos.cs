using Silky.Platform.Core.Dtos;

namespace Silky.Platform.Identity.Application.Contracts.Roles;

public class RoleDto
{
    public long Id { get; set; }

    /// <summary>角色编码，如 superadmin / admin</summary>
    public string Code { get; set; }

    public string Name { get; set; }

    public string Remark { get; set; }

    /// <summary>状态：0 禁用，1 启用</summary>
    public int Status { get; set; }

    public DateTime CreatedTime { get; set; }
}

public class GetRolePageInput : PagedQueryInput
{
    public int? Status { get; set; }
}

public class CreateRoleInput
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string Remark { get; set; }

    public IReadOnlyList<long> PermissionIds { get; set; } = new List<long>();
}

public class UpdateRoleInput
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Remark { get; set; }

    public int Status { get; set; }

    public IReadOnlyList<long> PermissionIds { get; set; } = new List<long>();
}

public class SetRolePermissionsInput
{
    public long RoleId { get; set; }

    public IReadOnlyList<long> PermissionIds { get; set; } = new List<long>();
}
