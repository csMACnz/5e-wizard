using Microsoft.Playwright;

namespace CharacterWizard.E2ETests;

/// <summary>
/// End-to-end tests for the "My Characters" page ("/characters").
/// </summary>
public sealed class CharactersPageTests(BlazorServerFixture server) : E2ETestBase(server)
{
    [Fact]
    public async Task CharactersPage_ShowsHeading()
    {
        await NavigateAndWaitForBlazorAsync("/characters");

        var heading = Page.Locator("h4", new PageLocatorOptions { HasTextString = "My Characters" });
        await Expect(heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CharactersPage_ShowsEmptyStateWhenNoCharacters()
    {
        // Each test runs in a fresh browser context (no local storage), so the page
        // should always show the empty state here.
        await NavigateAndWaitForBlazorAsync("/characters");

        var emptyMessage = Page.Locator("text=No saved characters yet");
        await Expect(emptyMessage).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CharactersPage_HasStartNewCharacterButton()
    {
        await NavigateAndWaitForBlazorAsync("/characters");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await Expect(button).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CharactersPage_HasBackToHomeLink()
    {
        await NavigateAndWaitForBlazorAsync("/characters");

        var backLink = Page.Locator("a", new PageLocatorOptions { HasTextString = "Back to Home" });
        await Expect(backLink).ToBeVisibleAsync();
    }

    // Convenience wrapper
    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}

