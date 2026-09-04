using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class CreatePostRequest
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public int UserId { get; set; } = 1;
}

/// <summary>
/// Live calls to https://jsonplaceholder.typicode.com
/// </summary>
public interface IPostApi
{
    [Get("/posts")]
    Task<List<Post>> ListAsync(CancellationToken cancellationToken = default);

    [Get("/posts/{id}")]
    Task<Post> GetAsync(int id, CancellationToken cancellationToken = default);

    [Post("/posts")]
    Task<Post> CreateAsync([Body] CreatePostRequest request, CancellationToken cancellationToken = default);
}
