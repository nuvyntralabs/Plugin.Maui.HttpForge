using System.Text;
using System.Text.Json;
using Plugin.Maui.HttpForge;
using Plugin.Maui.HttpForge.Sample.Demo;

namespace Plugin.Maui.HttpForge.Sample;

public partial class MainPage : ContentPage
{
    private readonly IPostApi _posts;
    private readonly IHttpBinApi _httpBin;
    private readonly ISseApi _sse;

    public MainPage(IPostApi posts, IHttpBinApi httpBin, ISseApi sse)
    {
        InitializeComponent();
        _posts = posts;
        _httpBin = httpBin;
        _sse = sse;
    }

    private async void OnListClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /posts  (PathPrefix)", async () =>
        {
            var posts = await _posts.ListAsync();
            return string.Join(Environment.NewLine, posts.Take(8).Select(p => $"{p.Id}. {p.Title}"));
        });
    }

    private async void OnGetClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /posts/1", async () =>
        {
            var post = await _posts.GetAsync(1);
            return $"{post.Id}. {post.Title}{Environment.NewLine}{post.Body}";
        });
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST /posts", async () =>
        {
            var created = await _posts.CreateAsync(new CreatePostRequest
            {
                Title = "HttpForge sample",
                Body = "Posted from Plugin.Maui.HttpForge 1.1",
                UserId = 1
            });
            return $"Created id {created.Id}: {created.Title}";
        });
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST httpbin /post (file)", async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("demo-photo"));
            var response = await _httpBin.UploadAsync(new StreamPart(stream, "photo.jpg", "image/jpeg"));
            return FormatEcho(response);
        });
    }

    private async void OnQueryObjectClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /posts?userId=1  (query object + camelCase)", async () =>
        {
            var posts = await _posts.SearchAsync(new PostQuery { UserId = 1 });
            return $"{posts.Count} posts for user 1{Environment.NewLine}" +
                   string.Join(Environment.NewLine, posts.Take(5).Select(p => $"{p.Id}. {p.Title}"));
        });
    }

    private async void OnEchoQueryClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET httpbin /get  DisplayName+Page", async () =>
        {
            var echo = await _httpBin.SearchAsync(new EchoQuery { DisplayName = "tea", Page = 2 });
            return FormatEcho(echo);
        });
    }

    private async void OnAgesMultiClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET httpbin /get?ages=1&ages=2", async () =>
        {
            var echo = await _httpBin.SearchAgesAsync([1, 2]);
            return FormatEcho(echo);
        });
    }

    private async void OnAgesCsvClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET httpbin /get?ages=1,2", async () =>
        {
            var echo = await _httpBin.SearchAgesCsvAsync([1, 2]);
            return FormatEcho(echo);
        });
    }

    private async void OnCommentsClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /posts/1/comments  (PathPrefix)", async () =>
        {
            var comments = await _posts.ListCommentsAsync(1);
            return string.Join(Environment.NewLine, comments.Take(5).Select(c => $"{c.Id}. {c.Email}"));
        });
    }

    private async void OnOrdersWithIdClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /anything/users/4/orders/9", async () =>
        {
            var echo = await _httpBin.GetOrdersAsync(4, 9);
            return FormatEcho(echo);
        });
    }

    private async void OnOrdersWithoutIdClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /anything/users/4/orders  (optional segment omitted)", async () =>
        {
            var echo = await _httpBin.GetOrdersAsync(4, null);
            return FormatEcho(echo);
        });
    }

    private async void OnAbsoluteUrlClicked(object? sender, EventArgs e)
    {
        await RunAsync("[Url] https://httpbin.org/get?from=url-attribute", async () =>
        {
            var echo = await _httpBin.GetFromAbsoluteAsync("https://httpbin.org/get?from=url-attribute");
            return FormatEcho(echo);
        });
    }

    private async void OnFlagsClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /get?archived  ([QueryName])", async () =>
        {
            var echo = await _httpBin.ListFlagsAsync("archived", archived: true);
            return FormatEcho(echo);
        });
    }

    private async void OnTimeoutClicked(object? sender, EventArgs e)
    {
        await RunAsync("[Timeout 2s] GET /delay/8", async () =>
        {
            try
            {
                await _httpBin.SlowAsync();
                return "Unexpected: the delay call finished. httpbin may have been faster than 2s.";
            }
            catch (OperationCanceledException)
            {
                return "Cancelled after 2s. Expected — the server would have waited 8s.";
            }
        });
    }

    private async void OnAuthClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /get  AuthorizationHeaderValueGetter", async () =>
        {
            var echo = await _httpBin.GetWithAuthAsync();
            var authorization = ReadHeader(echo, "Authorization") ?? "(missing)";
            return $"Authorization: {authorization}{Environment.NewLine}{FormatEcho(echo)}";
        });
    }

    private async void OnFormObjectClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST /post  [FormObject]", async () =>
        {
            var echo = await _httpBin.SaveProfileAsync(new ProfileForm
            {
                Name = "Ada",
                Address = new AddressForm { City = "London" }
            });
            return FormatEcho(echo);
        });
    }

    private async void OnGzipClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST /post  gzip body", async () =>
        {
            var echo = await _httpBin.CreateCompressedAsync(new CreatePostRequest
            {
                Title = "compressed",
                Body = "gzip from HttpForge",
                UserId = 1
            });
            var encoding = ReadHeader(echo, "Content-Encoding")
                          ?? "(httpbin may omit if it already decompressed)";
            return $"Content-Encoding: {encoding}{Environment.NewLine}{FormatEcho(echo)}";
        });
    }

    private async void OnJsonLinesClicked(object? sender, EventArgs e)
    {
        await RunAsync("POST /post  JSON Lines", async () =>
        {
            var echo = await _httpBin.SendBatchAsync(
            [
                new EventItem { Id = 1, Name = "one" },
                new EventItem { Id = 2, Name = "two" }
            ]);
            return FormatEcho(echo);
        });
    }

    private async void OnStreamClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET /stream/3  IAsyncEnumerable", async () =>
        {
            var lines = new List<string>();
            await foreach (var item in _httpBin.StreamEventsAsync())
                lines.Add($"{item.Id}  {item.Url}");

            return $"{lines.Count} streamed objects{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        });
    }

    private async void OnSseClicked(object? sender, EventArgs e)
    {
        await RunAsync("GET Wikimedia recentchange  SSE", async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var lines = new List<string>();
            try
            {
                await foreach (var item in _sse.StreamAsync(cts.Token))
                {
                    lines.Add($"{item.Type}  {item.Title}  ({item.User})".Trim());
                    if (lines.Count >= 3)
                        break;
                }
            }
            catch (OperationCanceledException) when (lines.Count > 0)
            {
                // Took a few events, then our 8s cap — still a success.
            }

            return lines.Count == 0
                ? "No SSE events in 8s. Try again, or use the JSON Lines stream button."
                : $"{lines.Count} SSE events{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        });
    }

    private async Task RunAsync(string title, Func<Task<string>> action)
    {
        SetBusy(true);
        StatusLabel.Text = $"{title}…";
        try
        {
            StatusLabel.Text = $"{title}{Environment.NewLine}{await action()}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{title} failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        foreach (var child in ButtonHost.Children)
        {
            if (child is Button button)
                button.IsEnabled = !busy;
        }
    }

    private static string FormatEcho(HttpBinResponse response)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(response.Url))
            builder.AppendLine(response.Url);

        AppendMap(builder, "args", response.Args);
        AppendMap(builder, "form", response.Form);
        AppendMap(builder, "files", response.Files);

        if (response.Headers is not null)
        {
            foreach (var key in new[] { "Authorization", "Content-Encoding", "Content-Type", "Accept" })
            {
                var value = ReadHeader(response, key);
                if (value is not null)
                    builder.Append(key).Append(": ").AppendLine(value);
            }
        }

        if (!string.IsNullOrWhiteSpace(response.Data))
        {
            var data = response.Data.Length > 240 ? response.Data[..240] + "…" : response.Data;
            builder.AppendLine("data: " + data);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendMap(StringBuilder builder, string title, Dictionary<string, JsonElement>? map)
    {
        if (map is null || map.Count == 0)
            return;

        builder.Append(title).Append(": ");
        builder.AppendLine(string.Join(", ", map.Select(pair => $"{pair.Key}={FormatElement(pair.Value)}")));
    }

    private static string? ReadHeader(HttpBinResponse response, string name)
    {
        if (response.Headers is null)
            return null;

        foreach (var pair in response.Headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                return FormatElement(pair.Value);
        }

        return null;
    }

    private static string FormatElement(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Undefined or JsonValueKind.Null => "",
            _ => element.GetRawText()
        };
}
