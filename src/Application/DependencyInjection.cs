using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.Auth;
using FiapX.Application.UseCases.Batch;
using FiapX.Application.UseCases.VideoProcessing;
using FiapX.Application.UseCases.VideoUpload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FiapX.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGetBatchStatusUseCase, GetBatchStatusUseCase>();
        services.AddScoped<IUploadVideoUseCase, UploadVideoUseCase>();
        services.AddScoped<IProcessVideoUseCase, ProcessVideoUseCase>();
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<ILoginUseCase, LoginUseCase>();

        return services;
    }
}