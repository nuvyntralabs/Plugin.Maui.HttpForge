using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class HttpBinResponse
{
    public string? Url { get; set; }
    public Dictionary<string, string>? Files { get; set; }
}

/// <summary>
/// Live multipart POST to https://httpbin.org/post
/// </summary>
public interface IHttpBinApi
{
    [Multipart]
    [Post("/post")]
    Task<HttpBinResponse> UploadAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);
}
