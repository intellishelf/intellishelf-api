using Intellishelf.Api.ImageProcessing;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Api.Services;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Services;
using Intellishelf.Domain.Files.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class BooksExtensions
{
    public static IServiceCollection AddBooks(this IServiceCollection services)
    {
        services.AddSingleton<IBookEntityMapper, BookEntityMapper>();
        services.AddSingleton<IBookMapper, BookMapper>();
        services.AddTransient<IBookDao, BookDao>();
        services.AddTransient<IBookService, BookService>();
        services.AddSingleton<IImageFileValidator, ImageFileValidator>();
        services.AddSingleton<IImageFileProcessor, ImageFileProcessor>();

        // ISBN import services
        services.AddHttpClient<IBookMetadataService, GoogleBooksMetadataService>();
        services.AddHttpClient<IHttpImageDownloader, HttpImageDownloader>();

        return services;
    }
}
