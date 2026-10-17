using Microsoft.Extensions.Logging;
using Quartz;

namespace Silky.Platform.Job.Application.Jobs;

/// <summary>
/// 内置 HTTP 调用 Job：按 JobDataMap 中的 url/method/headers/body 调用远端接口。
/// JobData 格式（JSON）：{ "url":"http://...", "method":"POST", "headers":{"Authorization":"Bearer x"}, "body":"...", "timeout":30000 }
/// </summary>
public class HttpCallJob : IJob
{
    private readonly ILogger<HttpCallJob> _logger;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(120) };

    public HttpCallJob(ILogger<HttpCallJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap.GetString("jobData");
        if (string.IsNullOrWhiteSpace(data))
            throw new InvalidOperationException("HttpCallJob requires 'jobData' (json).");

        var cfg = System.Text.Json.JsonSerializer.Deserialize<HttpCallConfig>(data);
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.Url))
            throw new InvalidOperationException("HttpCallJob config.url is required.");

        using var req = new HttpRequestMessage(
            string.IsNullOrWhiteSpace(cfg.Method) ? HttpMethod.Get : new HttpMethod(cfg.Method),
            cfg.Url);

        if (cfg.Headers != null)
        {
            foreach (var kv in cfg.Headers)
            {
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(cfg.Body))
        {
            req.Content = new StringContent(cfg.Body, System.Text.Encoding.UTF8, "application/json");
        }

        using var cts = new CancellationTokenSource(cfg.Timeout > 0 ? cfg.Timeout : 30000);
        var resp = await HttpClient.SendAsync(req, cts.Token);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(cts.Token);
        _logger.LogInformation("HttpCallJob -> {Url} {Status} {BodyPreview}",
            cfg.Url, (int)resp.StatusCode, body.Length > 200 ? body[..200] : body);
    }
}

public class HttpCallConfig
{
    public string Url { get; set; }
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
    public int Timeout { get; set; } = 30000;
}

/// <summary>
/// 示例 Job：简单日志输出，用于验证调度链路连通。
/// </summary>
public class SampleJob : IJob
{
    private readonly ILogger<SampleJob> _logger;
    public SampleJob(ILogger<SampleJob> logger) { _logger = logger; }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SampleJob fired at {Time}, JobKey={Key}", DateTime.Now, context.JobDetail.Key);
        return Task.CompletedTask;
    }
}
