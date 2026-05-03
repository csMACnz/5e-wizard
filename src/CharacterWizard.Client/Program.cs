using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CharacterWizard.Client;
using CharacterWizard.Client.Services;
using CharacterWizard.Shared.Utilities;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddSingleton<IRngFactory, SystemRngFactory>();
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<CharacterWizardState>();
builder.Services.AddScoped<BuildInfoService>();
builder.Services.AddScoped<RandomCharacterService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<CharacterSessionService>();
builder.Services.AddScoped<WizardContext>();
builder.Services.AddScoped<WizardCommitService>();
builder.Services.AddScoped<WizardStepValidator>();
builder.Services.AddScoped<WizardRandomizerService>();

await builder.Build().RunAsync();
