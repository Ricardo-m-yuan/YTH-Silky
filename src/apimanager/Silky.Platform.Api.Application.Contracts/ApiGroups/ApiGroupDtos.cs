namespace Silky.Platform.Api.Application.Contracts.ApiGroups;

public class ApiGroupDto
{
    public long Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }

    public DateTime CreatedTime { get; set; }
}

public class CreateApiGroupInput
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}

public class UpdateApiGroupInput
{
    public long Id { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}
