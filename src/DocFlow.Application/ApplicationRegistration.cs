using DocFlow.Application.Documents;
using DocFlow.Application.Documents.Download;
using DocFlow.Application.Documents.Processing;
using DocFlow.Application.Documents.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(new FileValidationOptions());
        services.AddScoped<FileValidationPolicy>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<IDocumentDownloadService, DocumentDownloadService>();
        return services;
    }
}
