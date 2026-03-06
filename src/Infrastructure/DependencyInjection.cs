using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.Batch;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Notifications;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Core.Interfaces.VideoFramerExtractor;
using FiapX.Infrastructure.BaseRepository;
using FiapX.Infrastructure.Data;
using FiapX.Infrastructure.Repositories;
using FiapX.Infrastructure.Services;
using FiapX.Infrastructure.Services.Notifications;
using FiapX.Infrastructure.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
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

            if (isDevelopment)
            {
                var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05);
                return new BlobServiceClient(settings.Storage.ConnectionString, options);
            }

            return new BlobServiceClient(settings.Storage.ConnectionString);
        });
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        services.AddSingleton(x => new ServiceBusClient(settings.ServiceBus.Connection));
        services.AddSingleton<IMessagePublisher, AzureServiceBusService>();

        services.AddScoped<IVideoFrameExtractorService, FFMpegFrameExtractorService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserContext, UserContextService>();

        services.AddSingleton(x =>
        {

            return new EmailClient(settings.AzureCommunicationServices.ConnectionString);
        });

        services.AddScoped<IEmailNotificationService, AzureEmailNotificationService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<INotifyBatchProcessingResultUseCase, NotifyBatchProcessingResultUseCase>();

        return services;
    }
}