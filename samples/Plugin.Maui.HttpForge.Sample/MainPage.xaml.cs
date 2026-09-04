using System.Text;
using Plugin.Maui.HttpForge;
using Plugin.Maui.HttpForge.Sample.Demo;

namespace Plugin.Maui.HttpForge.Sample;

public partial class MainPage : ContentPage
{
    private readonly IPostApi _posts;
    private readonly IHttpBinApi _httpBin;

    public MainPage(IPostApi posts, IHttpBinApi httpBin)
    {
        InitializeComponent();
        _posts = posts;
        _httpBin = httpBin;
    }

    private async void OnListClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET https://jsonplaceholder.typicode.com/posts", async () =>
        {
            var posts = await _posts.ListAsync();
            return string.Join(Environment.NewLine, posts.Take(8).Select(p => $"{p.Id}. {p.Title}"));
        });
    }

    private async void OnGetClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET https://jsonplaceholder.typicode.com/posts/1", async () =>
        {
            var post = await _posts.GetAsync(1);
            return $"{post.Id}. {post.Title}{Environment.NewLine}{post.Body}";
        });
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST https://jsonplaceholder.typicode.com/posts", async () =>
        {
            var created = await _posts.CreateAsync(new CreatePostRequest
            {
                Title = "HttpForge sample",
                Body = "Posted from Plugin.Maui.HttpForge",
                UserId = 1
            });
            return $"Created id {created.Id}: {created.Title}";
        });
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST https://httpbin.org/post", async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("demo-photo"));
            var response = await _httpBin.UploadAsync(new StreamPart(stream, "photo.jpg", "image/jpeg"));
            var files = response.Files is { Count: > 0 }
                ? string.Join(", ", response.Files.Keys)
                : "(none)";
            return $"Echo from {response.Url}{Environment.NewLine}Files: {files}";
        });
    }

    private async Task RunAsync(string title, Func<Task<string>> action)
    {
        StatusLabel.Text = $"{title}…";
        try
        {
            StatusLabel.Text = $"{title}{Environment.NewLine}{await action()}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{title} failed: {ex.Message}";
        }
    }
}
