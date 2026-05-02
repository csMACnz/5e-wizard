namespace CharacterWizard.E2ETests;

/// <summary>
/// Marks the xunit collection that shares a single <see cref="BlazorServerFixture"/>
/// (and therefore a single running Blazor dev server) across all E2E test classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BlazorServerCollection : ICollectionFixture<BlazorServerFixture>
{
    public const string Name = "Blazor E2E";
}
