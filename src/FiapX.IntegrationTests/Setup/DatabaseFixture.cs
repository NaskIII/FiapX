using FiapX.Infrastructure.Data;
using FiapX.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FiapX.IntegrationTests.Setup;

public class DatabaseFixture : IAsyncLifetime
{
    public FiapXDbContext Context { get; private set; }
    public FiapXSettings Settings { get; private set; }

    private static readonly HttpClient _httpClient;

    static DatabaseFixture()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler);
    }

    public DatabaseFixture()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var builder = new ConfigurationBuilder()
            .AddJsonFile(path, optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        Settings = configuration.GetSection("FiapX").Get<FiapXSettings>()
                   ?? throw new Exception("Configuração FiapX não encontrada.");

        var options = new DbContextOptionsBuilder<FiapXDbContext>()
            .UseCosmos(
                Settings.Cosmos.ConnectionString,
                Settings.Cosmos.DatabaseName,
                cosmosOptions =>
                {
                    cosmosOptions.HttpClientFactory(() => _httpClient);

                    cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

                    cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(100));
                    cosmosOptions.LimitToEndpoint(true);
                }
            )
            .Options;

        Context = new FiapXDbContext(options);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();

            var created = await Context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"FATAL: Não foi possível conectar ao Cosmos Emulator em {Settings.Cosmos.ConnectionString}. \nVerifique se o Docker está rodando e se você consegue acessar https://localhost:8081/_explorer/index.html. \nErro: {ex.Message}", ex);
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.DisposeAsync();
        }
        catch
        {
            // Ignora erros na limpeza para não sujar o log de testes
        }
    }
}