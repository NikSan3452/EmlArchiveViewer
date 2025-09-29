using System.Text;
using Blazored.LocalStorage;
using EmlArchiveViewer.Components;
using EmlArchiveViewer.Services;
using MudBlazor.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddMudServices();
builder.Services.AddRadzenComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<EmlParserService>();
builder.Services.AddSingleton<EmlArchiveService>();
builder.Services.AddSingleton<ViewerStateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();