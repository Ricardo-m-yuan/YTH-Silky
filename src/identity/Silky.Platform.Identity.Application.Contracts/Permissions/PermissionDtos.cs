using Silky.Platform.Core.Dtos;

namespace Silky.Platform.Identity.Application.Contracts.Permissions;

/// <summary>权限类型：1 菜单 2 按钮 3 接口</summary>
public static class PermissionTypes
{
    public const int Menu = 1;
    public const int Button = 2;
    public const int Api = 3;
}

public class PermissionDto
{
    public long Id { get; set; }

    /// <summary>权限码，如 identity.user.create；与接口管理中的 PermissionCode 对应</summary>
    public string Code { get; set; }

    public string Name { get; set; }

    public int Type { get; set; }

    public string Group { get; set; }

    public long? ParentId { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }

    public DateTime CreatedTime { get; set; }
}

public class PermissionTreeNodeDto
{
    public long Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public int Type { get; set; }

    public string Group { get; set; }

    public long? ParentId { get; set; }

    public int Sort { get; set; }

    public IReadOnlyList<PermissionTreeNodeDto> Children { get; set; } = new List<PermissionTreeNodeDto>();
}

public class GetPermissionPageInput : PagedQueryInput
{
    public int? Type { get; set; }

    public string Group { get; set; }
}

public class CreatePermissionInput
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int Type { get; set; } = PermissionTypes.Api;

    public string Group { get; set; }

    public long? ParentId { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}

public class UpdatePermissionInput
{
    public long Id { get; set; }

    public string Name { get; set; }

    public int Type { get; set; }

    public string Group { get; set; }

    public long? ParentId { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}
