using Intellishelf.Api.Configuration;
using Intellishelf.Api.ImageProcessing;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Api.Services;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Services;

namespace Intellishelf.Api.Modules;

public static class BooksModule
{
    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IBookEntityMapper, BookEntityMapper>();
        services.AddSingleton<IBookMapper, BookMapper>();
        services.AddTransient<IBookDao, BookDao>();
        services.AddTransient<IBookService, BookService>();
        services.AddSingleton<IImageFileValidator, ImageFileValidator>();
        services.AddSingleton<IImageFileProcessor, ImageFileProcessor>();

        // Register ISBN lookup service with Google Books API key
        var googleBooksApiKey = configuration["GoogleBooksApi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(googleBooksApiKey))
        {
            services.AddSingleton<IIsbnLookupService>(sp => new IsbnLookupService(googleBooksApiKey));
        }
        else
        {
            throw new InvalidOperationException(
                "GoogleBooksApi:ApiKey is not configured. Please add it to appsettings.json or user secrets.");
        }
    }
}
