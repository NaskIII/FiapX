using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.Batch;
using FiapX.Application.UseCases.VideoUpload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGetBatchStatusUseCase, GetBatchStatusUseCase>();
        services.AddScoped<IUploadVideoUseCase, UploadVideoUseCase>();

        return services;
    }
}