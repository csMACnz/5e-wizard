using Microsoft.Playwright;

namespace CharacterWizard.E2ETests;

/// <summary>
/// End-to-end tests for creating a new character through the wizard.
/// </summary>
public sealed class WizardTests(BlazorServerFixture server) : E2ETestBase(server)
{
    [Fact]
    public async Task StartNewCharacter_NavigatesToWizardPage()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await button.ClickAsync();

        // The URL should now contain /wizard?session=
        await Page.WaitForURLAsync(url => url.Contains("/wizard") && url.Contains("session="),
            new PageWaitForURLOptions { Timeout = 10_000 });

        Assert.Contains("/wizard", Page.Url);
        Assert.Contains("session=", Page.Url);
    }

    [Fact]
    public async Task WizardStep1_ShowsCharacterMetaForm()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await button.ClickAsync();

        // Wait for Blazor to load the wizard and its data
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
    }

    [Fact]
    public async Task WizardStep1_HasCharacterNameField()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await button.ClickAsync();

        // Wait for step 1 to be displayed
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // The Character Name text field should be present
        var nameField = Page.Locator("label", new PageLocatorOptions { HasTextString = "Character Name" });
        await Expect(nameField).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WizardStep1_NextButtonDisabledWithoutName()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var button = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await button.ClickAsync();

        // Wait for step 1
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // "Next" button should be disabled when name is empty
        var nextButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Next" });
        await Expect(nextButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task WizardStep1_NextButtonEnabledAfterEnteringName()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var startButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await startButton.ClickAsync();

        // Wait for step 1
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // Enter a character name
        var nameInput = Page.Locator("input[aria-required='true']");
        await nameInput.FillAsync("Aragorn");

        // "Next" button should become enabled
        var nextButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Next" });
        await Expect(nextButton).ToBeEnabledAsync();
    }

    [Fact]
    public async Task WizardStep1_CanAdvanceToStep2()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var startButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await startButton.ClickAsync();

        // Wait for step 1
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // Fill in character name and click Next
        var nameInput = Page.Locator("input[aria-required='true']");
        await nameInput.FillAsync("Legolas");

        var nextButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Next" });
        await Expect(nextButton).ToBeEnabledAsync();
        await nextButton.ClickAsync();

        // Step 2 (Ability Scores) should now be shown
        var step2Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 2" });
        await Expect(step2Heading).ToBeVisibleAsync();
    }

    // Convenience wrapper
    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
