using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class CreatePostRequest
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public int UserId { get; set; } = 1;
}

public sealed class PostQuery
{
    public int UserId { get; set; }
}

/// <summary>
/// Live calls to https://jsonplaceholder.typicode.com with a shared <c>/posts</c> prefix.
/// </summary>
[PathPrefix("/posts")]
[Headers("Accept: application/json")]
public interface IPostApi
{
    [Get("/")]
    Task<List<Post>> ListAsync(CancellationToken cancellationToken = default);

    [Get("/")]
    Task<List<Post>> SearchAsync([Query] PostQuery query, CancellationToken cancellationToken = default);

    [Get("/{id}")]
    Task<Post> GetAsync(int id, CancellationToken cancellationToken = default);

    [Get("/{id}/comments")]
    Task<List<Comment>> ListCommentsAsync(int id, CancellationToken cancellationToken = default);

    [Post("/")]
    Task<Post> CreateAsync([Body] CreatePostRequest request, CancellationToken cancellationToken = default);
}
