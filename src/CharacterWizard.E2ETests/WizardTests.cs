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

    [Fact]
    public async Task NewCharacterMenuButton_WhileOnStep2_ResetsWizardToStep1WithEmptyForm()
    {
        await NavigateAndWaitForBlazorAsync("/");

        // Start a new character from the home page
        var startButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await startButton.ClickAsync();

        // Wait for step 1
        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // Fill in a character name and advance to step 2
        var nameInput = Page.Locator("input[aria-required='true']");
        await nameInput.FillAsync("Thorin");

        var nextButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Next" });
        await Expect(nextButton).ToBeEnabledAsync();
        await nextButton.ClickAsync();

        var step2Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 2" });
        await Expect(step2Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        // Capture the current session ID from the URL
        var urlBeforeReset = Page.Url;

        // MudNavLink with OnClick (no Href) renders the clickable inner element as
        // <div class="mud-nav-link"> — not as <a> or <button>.
        // When the drawer is closed, elements remain in the DOM but are positioned
        // outside the viewport via CSS transform. IsVisibleAsync() returns true in
        // this case (CSS visibility check only). Use JS bounding-box to detect it.
        var newCharacterNavItem = Page.Locator(".mud-nav-link")
            .Filter(new LocatorFilterOptions { HasText = "New Character" });

        var isInViewport = await newCharacterNavItem.EvaluateAsync<bool>(
            "el => { const r = el.getBoundingClientRect(); return r.top >= 0 && r.left >= 0 && r.bottom <= window.innerHeight && r.right <= window.innerWidth; }");

        if (!isInViewport)
        {
            await Page.Locator("button[aria-label='Toggle navigation menu']").ClickAsync();
            await Expect(newCharacterNavItem).ToBeInViewportAsync(new LocatorAssertionsToBeInViewportOptions { Timeout = 10_000 });
        }

        await newCharacterNavItem.ClickAsync();

        // The wizard should return to step 1
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        // The character name field should be empty (fresh session)
        var nameInputAfterReset = Page.Locator("input[aria-required='true']");
        await Expect(nameInputAfterReset).ToBeEmptyAsync();

        // The session ID in the URL should be different (new session)
        Assert.NotEqual(urlBeforeReset, Page.Url);
        Assert.Contains("session=", Page.Url);
    }

    [Fact]
    public async Task HomePageStartNew_AfterExistingSession_NavigatesToFreshWizard()
    {
        // Create a wizard session first (navigate from home, fill in a name, advance to step 2)
        await NavigateAndWaitForBlazorAsync("/");

        var startButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await startButton.ClickAsync();

        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        var nameInput = Page.Locator("input[aria-required='true']");
        await nameInput.FillAsync("Gandalf");

        var nextButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Next" });
        await Expect(nextButton).ToBeEnabledAsync();
        await nextButton.ClickAsync();

        var step2Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 2" });
        await Expect(step2Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        var firstSessionUrl = Page.Url;

        // Return to the home page via a full page reload (as a real user returning to "/"
        // after their browser history brings them back)
        await NavigateAndWaitForBlazorAsync("/");

        // Click "Start New Character" from the home page
        var startNewButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await Expect(startNewButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
        await startNewButton.ClickAsync();

        // The wizard should open at step 1
        await Page.WaitForURLAsync(url => url.Contains("/wizard") && url.Contains("session="),
            new PageWaitForURLOptions { Timeout = 10_000 });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // The character name field must be empty — not "Gandalf" from the previous session
        var nameInputAfterReset = Page.Locator("input[aria-required='true']");
        await Expect(nameInputAfterReset).ToBeEmptyAsync();

        // The URL session ID must differ from the first session
        Assert.NotEqual(firstSessionUrl, Page.Url);
        Assert.Contains("session=", Page.Url);
    }

    [Fact]
    public async Task CharactersPageStartNew_NavigatesToFreshWizard()
    {
        await NavigateAndWaitForBlazorAsync("/characters");

        // The Characters page should have a "Start New Character" button
        var startNewButton = Page.Locator("button", new PageLocatorOptions { HasTextString = "Start New Character" });
        await Expect(startNewButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
        await startNewButton.ClickAsync();

        // Should navigate to the wizard at step 1
        await Page.WaitForURLAsync(url => url.Contains("/wizard") && url.Contains("session="),
            new PageWaitForURLOptions { Timeout = 10_000 });

        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // The character name field should be empty
        var nameInput = Page.Locator("input[aria-required='true']");
        await Expect(nameInput).ToBeEmptyAsync();
    }

    [Fact]
    public async Task NewCharacterMenuButton_ClosesMenuDrawer()
    {
        await NavigateAndWaitForBlazorAsync("/");

        // Open the drawer
        await Page.Locator("button[aria-label='Toggle navigation menu']").ClickAsync();

        // Verify the "New Character" nav item is in the viewport (drawer is open)
        var newCharacterNavItem = Page.Locator(".mud-nav-link")
            .Filter(new LocatorFilterOptions { HasText = "New Character" });
        await Expect(newCharacterNavItem).ToBeInViewportAsync(new LocatorAssertionsToBeInViewportOptions { Timeout = 10_000 });

        // Click "New Character" in the menu
        await newCharacterNavItem.ClickAsync();

        // Wait for navigation to the wizard
        await Page.WaitForURLAsync(url => url.Contains("/wizard") && url.Contains("session="),
            new PageWaitForURLOptions { Timeout = 10_000 });

        // The drawer should now be closed: the nav item should no longer be in the viewport
        var isInViewport = await newCharacterNavItem.EvaluateAsync<bool>(
            "el => { const r = el.getBoundingClientRect(); return r.top >= 0 && r.left >= 0 && r.bottom <= window.innerHeight && r.right <= window.innerWidth; }");
        Assert.False(isInViewport, "The navigation drawer should have closed after clicking 'New Character'.");
    }

    [Fact]
    public async Task NewCharacterMenuButton_FromCharactersPage_NavigatesToFreshWizard()
    {
        await NavigateAndWaitForBlazorAsync("/characters");

        // Open the nav drawer and click "New Character"
        var newCharacterNavItem = Page.Locator(".mud-nav-link")
            .Filter(new LocatorFilterOptions { HasText = "New Character" });

        var isInViewport = await newCharacterNavItem.EvaluateAsync<bool>(
            "el => { const r = el.getBoundingClientRect(); return r.top >= 0 && r.left >= 0 && r.bottom <= window.innerHeight && r.right <= window.innerWidth; }");
        if (!isInViewport)
        {
            await Page.Locator("button[aria-label='Toggle navigation menu']").ClickAsync();
            await Expect(newCharacterNavItem).ToBeInViewportAsync(new LocatorAssertionsToBeInViewportOptions { Timeout = 10_000 });
        }

        await newCharacterNavItem.ClickAsync();

        // Should navigate to the wizard at step 1
        await Page.WaitForURLAsync(url => url.Contains("/wizard") && url.Contains("session="),
            new PageWaitForURLOptions { Timeout = 10_000 });

        var step1Heading = Page.Locator("h5", new PageLocatorOptions { HasTextString = "Step 1" });
        await Expect(step1Heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // The character name field should be empty (fresh session)
        var nameInput = Page.Locator("input[aria-required='true']");
        await Expect(nameInput).ToBeEmptyAsync();
    }

    // Convenience wrapper
    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
