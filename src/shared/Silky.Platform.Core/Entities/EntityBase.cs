namespace Silky.Platform.Core.Entities;

/// <summary>
/// 实体基类：主键 + 审计字段 + 软删除
/// </summary>
public abstract class EntityBase
{
    public long Id { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.Now;

    public long? CreatorId { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public long? UpdaterId { get; set; }

    public bool IsDeleted { get; set; }
}
