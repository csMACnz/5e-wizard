using Microsoft.Playwright;

namespace CharacterWizard.E2ETests;

/// <summary>
/// Helper base class that creates a Playwright browser, context and page for each test
/// method, then disposes them in <see cref="DisposeAsync"/>.
/// </summary>
[Collection(BlazorServerCollection.Name)]
public abstract class E2ETestBase : IAsyncLifetime
{
    protected readonly BlazorServerFixture Server;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    protected E2ETestBase(BlazorServerFixture server)
    {
        Server = server;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        Context = await _browser.NewContextAsync();
        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();
        await _browser!.CloseAsync();
        _playwright!.Dispose();
    }

    /// <summary>
    /// Navigates to <paramref name="path"/> relative to the server base URL and waits
    /// until the Blazor loading spinner is gone, indicating the app has hydrated.
    /// </summary>
    protected async Task NavigateAndWaitForBlazorAsync(string path = "/")
    {
        await Page.GotoAsync($"{Server.BaseUrl}{path}");
        // Wait for the initial SVG loading-progress circle to be removed from the DOM
        await Page.WaitForSelectorAsync(".loading-progress", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 60_000,
        });
    }
}
