using Silky.Platform.Core.Entities;

namespace Silky.Platform.Identity.Application.Entities;

/// <summary>用户</summary>
public class User : EntityBase
{
    public string UserName { get; set; }

    /// <summary>PBKDF2 密码摘要</summary>
    public string PasswordHash { get; set; }

    /// <summary>密码盐</summary>
    public string PasswordSalt { get; set; }

    public string RealName { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    /// <summary>状态：0 禁用，1 启用</summary>
    public int Status { get; set; } = 1;

    public string Remark { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public string LastLoginIp { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>角色</summary>
public class Role : EntityBase
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string Remark { get; set; }

    /// <summary>状态：0 禁用，1 启用</summary>
    public int Status { get; set; } = 1;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>权限（菜单/按钮/接口）</summary>
public class Permission : EntityBase
{
    public string Code { get; set; }

    public string Name { get; set; }

    /// <summary>见 PermissionTypes</summary>
    public int Type { get; set; }

    public string Group { get; set; }

    public long? ParentId { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>用户-角色关联</summary>
public class UserRole
{
    public long UserId { get; set; }

    public long RoleId { get; set; }

    public User User { get; set; }

    public Role Role { get; set; }
}

/// <summary>角色-权限关联</summary>
public class RolePermission
{
    public long RoleId { get; set; }

    public long PermissionId { get; set; }

    public Role Role { get; set; }

    public Permission Permission { get; set; }
}
