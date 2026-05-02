using Microsoft.Playwright;

namespace CharacterWizard.E2ETests;

/// <summary>
/// End-to-end tests for the home page ("/").
/// </summary>
public sealed class HomePageTests(BlazorServerFixture server) : E2ETestBase(server)
{
    [Fact]
    public async Task HomePage_ShowsAppTitle()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var heading = Page.Locator("h3");
        await Expect(heading).ToContainTextAsync("5e Character Wizard");
    }

    [Fact]
    public async Task HomePage_HasStartNewCharacterButton()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await Expect(button).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_HasRollRandomCharacterButton()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Roll Random Character" });
        await Expect(button).ToBeVisibleAsync();
    }

    // Convenience wrapper so test methods can use Expect without a static import
    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
