using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Core.Interfaces.VideoFramerExtractor;
using FiapX.Infrastructure.BaseRepository;
using FiapX.Infrastructure.Data;
using FiapX.Infrastructure.Repositories;
using FiapX.Infrastructure.Services;
using FiapX.Infrastructure.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FiapX.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("FiapX").Get<FiapXSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException("A seção de configuração 'FiapX' não foi encontrada ou está vazia.");
        }

        services.AddSingleton(settings);

        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<FiapXDbContext>(options =>
            options.UseCosmos(
                settings.Cosmos.ConnectionString,
                settings.Cosmos.DatabaseName,
                cosmosOptions =>
                {
                    cosmosOptions.ConnectionMode(ConnectionMode.Gateway);

                    if (isDevelopment)
                    {
                        cosmosOptions.LimitToEndpoint(true);
                        cosmosOptions.HttpClientFactory(() =>
                        {
                            HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                            {
                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            };
                            return new HttpClient(httpMessageHandler);
                        });
                    }
                }
        ));

        services.AddScoped<IVideoBatchRepository, VideoBatchRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(x =>
        {
            var settings = x.GetRequiredService<FiapXSettings>();
            return new BlobServiceClient(settings.Storage.ConnectionString);
        });
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        services.AddSingleton(x => new ServiceBusClient(settings.ServiceBus.Connection));
        services.AddSingleton<IMessagePublisher, AzureServiceBusService>();

        services.AddScoped<IVideoFrameExtractorService, FFMpegFrameExtractorService>();

        return services;
    }
}