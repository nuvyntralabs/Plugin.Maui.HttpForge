using System.Text;
using Plugin.Maui.HttpForge;
using Plugin.Maui.HttpForge.Sample.Demo;

namespace Plugin.Maui.HttpForge.Sample;

public partial class MainPage : ContentPage
{
    private readonly ICatalogApi _api;

    public MainPage(ICatalogApi api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnListClicked(object? sender, EventArgs e)
    {
        await RunAsync("List", async () =>
        {
            var products = await _api.ListAsync();
            return string.Join(Environment.NewLine, products.Select(p => $"{p.Id}. {p.Name}"));
        });
    }

    private async void OnGetClicked(object? sender, EventArgs e)
    {
        await RunAsync("Get", async () =>
        {
            var product = await _api.GetAsync(1);
            return $"{product.Id}. {product.Name}";
        });
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        await RunAsync("Create", async () =>
        {
            var product = await _api.CreateAsync(new CreateProductRequest { Name = "Masala chai" });
            return $"Created {product.Id}. {product.Name}";
        });
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        await RunAsync("Upload", async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("demo-photo"));
            await _api.UploadPhotoAsync(1, new StreamPart(stream, "photo.jpg", "image/jpeg"));
            return "Uploaded photo.jpg as multipart/form-data.";
        });
    }

    private async Task RunAsync(string title, Func<Task<string>> action)
    {
        try
        {
            StatusLabel.Text = $"{title}:{Environment.NewLine}{await action()}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{title} failed: {ex.Message}";
        }
    }
}
