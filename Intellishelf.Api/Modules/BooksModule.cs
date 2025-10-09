using Intellishelf.Api.Mappers.Books;
using Intellishelf.Api.Validators;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Services;

namespace Intellishelf.Api.Modules;

public static class BooksModule
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<IBookEntityMapper, BookEntityMapper>();
        services.AddSingleton<IBookMapper, BookMapper>();
        services.AddTransient<IBookDao, BookDao>();
        services.AddTransient<IBookService, BookService>();
        services.AddSingleton<IImageFileValidator, ImageFileValidator>();
    }
}