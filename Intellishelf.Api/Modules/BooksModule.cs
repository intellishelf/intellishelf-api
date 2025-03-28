using Intellishelf.Api.Mappers.Books;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Services;

namespace Intellishelf.Api.Modules;

public static class BooksModule
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<IBooksMapper, BooksMapper>();
        services.AddTransient<IBookDao, BookDao>();
        services.AddTransient<IBookService, BookService>();
    }
}