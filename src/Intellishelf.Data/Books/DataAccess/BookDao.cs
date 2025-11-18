using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace Intellishelf.Data.Books.DataAccess;

public class BookDao(IMongoDatabase database, IBookEntityMapper mapper) : IBookDao
{
    private readonly IMongoCollection<BookEntity> _booksCollection = database.GetCollection<BookEntity>(BookEntity.CollectionName);

    public async Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var books = await _booksCollection
            .Find(b => b.UserId == userIdObject)
            .ToListAsync();

        var result = books.Select(mapper.Map).ToList();

        return result;
    }

    public async Task<TryResult<PagedResult<Book>>> GetPagedBooksAsync(string userId, BookQueryParameters queryParameters)
    {
        var filter = Builders<BookEntity>.Filter.Eq(b => b.UserId, ObjectId.Parse(userId));

        // Get total count for pagination
        var totalCount = await _booksCollection.CountDocumentsAsync(filter);

        // Build sort definition based on orderBy parameter
        var sortDefinition = BuildSortDefinition(queryParameters.OrderBy, queryParameters.Ascending);

        // Get paged data
        var books = await _booksCollection
            .Find(filter)
            .Sort(sortDefinition)
            .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
            .Limit(queryParameters.PageSize)
            .ToListAsync();

        var mappedBooks = books.Select(mapper.Map).ToList();

        var pagedResult = new PagedResult<Book>(
            mappedBooks,
            totalCount,
            queryParameters.Page,
            queryParameters.PageSize);

        return pagedResult;
    }

    private static SortDefinition<BookEntity> BuildSortDefinition(BookOrderBy orderBy, bool ascending)
    {
        SortDefinition<BookEntity> sortDefinition;

        switch (orderBy)
        {
            case BookOrderBy.Title:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.Title)
                    : Builders<BookEntity>.Sort.Descending(b => b.Title);
                break;
            case BookOrderBy.Author:
                // Sort by the last author in the array if it exists
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.Authors != null && b.Authors.Length > 0 ? b.Authors.FirstOrDefault() : string.Empty)
                    : Builders<BookEntity>.Sort.Descending(b => b.Authors != null && b.Authors.Length > 0 ? b.Authors.FirstOrDefault() : string.Empty);
                break;
            case BookOrderBy.Published:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.PublicationDate)
                    : Builders<BookEntity>.Sort.Descending(b => b.PublicationDate);
                break;
            case BookOrderBy.Added:
            default:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.CreatedDate)
                    : Builders<BookEntity>.Sort.Descending(b => b.CreatedDate);
                break;
        }

        return sortDefinition;
    }

    public async Task<TryResult<Book>> GetBookAsync(string userId, string bookId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var bookEntity = await _booksCollection
            .Find(b => b.Id == bookId && b.UserId == userIdObject)
            .FirstOrDefaultAsync();

        if (bookEntity == null)
            return new Error(BookErrorCodes.BookNotFound, "Book not found");

        return mapper.Map(bookEntity);
    }

    public async Task<TryResult<Book?>> FindByIsbnAsync(string userId, string? isbn10, string? isbn13)
    {
        var userIdObject = ObjectId.Parse(userId);

        var bookEntity = await _booksCollection
            .Find(b => b.UserId == userIdObject &&
                      ((isbn10 != null && b.Isbn10 == isbn10) ||
                       (isbn13 != null && b.Isbn13 == isbn13)))
            .FirstOrDefaultAsync();

        return bookEntity != null ? mapper.Map(bookEntity) : null;
    }

    public async Task<TryResult<Book>> AddBookAsync(AddBookRequest request)
    {
        var book = new BookEntity
        {
            UserId = ObjectId.Parse(request.UserId),
            Title = request.Title,
            Authors = request.Authors,
            PublicationDate = request.PublicationDate,
            Isbn10 = request.Isbn10,
            Isbn13 = request.Isbn13,
            Tags = request.Tags,
            Annotation = request.Annotation,
            Description = request.Description,
            Publisher = request.Publisher,
            Pages = request.Pages,
            CoverImageUrl = request.CoverImageUrl,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Status = request.Status,
            StartedReadingDate = request.StartedReadingDate,
            FinishedReadingDate = request.FinishedReadingDate
        };

        await _booksCollection.InsertOneAsync(book);

        return mapper.Map(book);
    }

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var userIdObject = ObjectId.Parse(request.UserId);

        var filter = Builders<BookEntity>
            .Filter
            .Where(b => b.Id == request.Id && b.UserId == userIdObject);

        var update = Builders<BookEntity>.Update;
        var updates = new List<UpdateDefinition<BookEntity>>
        {
            update.Set(b => b.Title, request.Title),
            update.Set(b => b.Authors, request.Authors),
            update.Set(b => b.PublicationDate, request.PublicationDate),
            update.Set(b => b.Isbn10, request.Isbn10),
            update.Set(b => b.Isbn13, request.Isbn13),
            update.Set(b => b.Tags, request.Tags),
            update.Set(b => b.Annotation, request.Annotation),
            update.Set(b => b.Description, request.Description),
            update.Set(b => b.Publisher, request.Publisher),
            update.Set(b => b.Pages, request.Pages),
            update.Set(b => b.Status, request.Status),
            update.Set(b => b.StartedReadingDate, request.StartedReadingDate),
            update.Set(b => b.FinishedReadingDate, request.FinishedReadingDate),
            update.CurrentDate(b => b.ModifiedDate)
        };

        if (request.CoverImageUrl != null)
        {
            updates.Add(update.Set(b => b.CoverImageUrl, request.CoverImageUrl));
        }

        var combinedUpdate = update.Combine(updates);

        var result = await _booksCollection.UpdateOneAsync(filter, combinedUpdate);

        return result.MatchedCount == 0
            ? new Error(BookErrorCodes.BookNotFound, "Book not found or no permission to update")
            : TryResult.Success();
    }

    public async Task<TryResult> UpdateEmbeddingAsync(string userId, string bookId, float[] embedding)
    {
        var userIdObject = ObjectId.Parse(userId);

        var filter = Builders<BookEntity>
            .Filter
            .Where(b => b.Id == bookId && b.UserId == userIdObject);

        var update = Builders<BookEntity>.Update
            .Set(b => b.Embedding, embedding)
            .CurrentDate(b => b.ModifiedDate);

        var result = await _booksCollection.UpdateOneAsync(filter, update);

        return result.MatchedCount == 0
            ? new Error(BookErrorCodes.BookNotFound, "Book not found or no permission to update")
            : TryResult.Success();
    }

    public async Task<TryResult> DeleteBookAsync(string userId, string bookId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var result = await _booksCollection.DeleteOneAsync(b => b.Id == bookId && b.UserId == userIdObject);

        return result.DeletedCount == 0
            ? new Error(BookErrorCodes.BookNotFound, "Book not found or no permission to delete")
            : TryResult.Success();
    }

    public async Task<TryResult<PagedResult<Book>>> SearchAsync(string userId, SearchQueryParameters queryParameters)
    {
        var userObjectId = ObjectId.Parse(userId);

        // Use RRF (Reciprocal Rank Fusion) for hybrid search when embeddings are available
        if (queryParameters.SearchEmbedding != null && queryParameters.SearchEmbedding.Length > 0)
        {
            return await SearchWithRRFAsync(userObjectId, queryParameters);
        }

        // Fall back to text-only search if no embeddings
        return await SearchTextOnlyAsync(userObjectId, queryParameters);
    }

    private async Task<TryResult<PagedResult<Book>>> SearchWithRRFAsync(ObjectId userId, SearchQueryParameters queryParameters)
    {
        const int textPriority = 1;    // Lower = higher importance (exact matches ranked higher)
        const int vectorPriority = 60; // Higher = lower importance (semantic matches ranked lower)
        const int numCandidates = 100;

        // Build the RRF pipeline using raw BsonDocuments for $vectorSearch support
        var pipeline = new List<BsonDocument>();

        // 1. Vector Search Pipeline
        var vectorSearchStage = new BsonDocument("$vectorSearch", new BsonDocument
        {
            { "index", "vector_index" },
            { "path", "Embedding" },
            { "queryVector", new BsonArray(queryParameters.SearchEmbedding!.Select(f => new BsonDouble(f))) },
            { "numCandidates", numCandidates },
            { "limit", queryParameters.PageSize * 2 }, // Get more candidates for better RRF
            { "filter", BuildFilterDocument(userId, queryParameters.Status) }
        });

        pipeline.Add(vectorSearchStage);

        // 2. Group into array for ranking
        pipeline.Add(new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "docs", new BsonDocument("$push", "$$ROOT") }
        }));

        // 3. Unwind with array index for ranking
        pipeline.Add(new BsonDocument("$unwind", new BsonDocument
        {
            { "path", "$docs" },
            { "includeArrayIndex", "rank" }
        }));

        // 4. Compute vector search RRF score
        pipeline.Add(new BsonDocument("$addFields", new BsonDocument
        {
            { "vs_score", new BsonDocument("$divide", new BsonArray
                {
                    1.0,
                    new BsonDocument("$add", new BsonArray { "$rank", vectorPriority, 1 })
                })
            }
        }));

        // 5. Project fields with vector score
        pipeline.Add(new BsonDocument("$project", new BsonDocument
        {
            { "vs_score", 1 },
            { "_id", "$docs._id" },
            { "Title", "$docs.Title" },
            { "Authors", "$docs.Authors" },
            { "Description", "$docs.Description" },
            { "Publisher", "$docs.Publisher" },
            { "Tags", "$docs.Tags" },
            { "Annotation", "$docs.Annotation" },
            { "Isbn10", "$docs.Isbn10" },
            { "Isbn13", "$docs.Isbn13" },
            { "Pages", "$docs.Pages" },
            { "CoverImageUrl", "$docs.CoverImageUrl" },
            { "PublicationDate", "$docs.PublicationDate" },
            { "UserId", "$docs.UserId" },
            { "Status", "$docs.Status" },
            { "StartedReadingDate", "$docs.StartedReadingDate" },
            { "FinishedReadingDate", "$docs.FinishedReadingDate" },
            { "CreatedDate", "$docs.CreatedDate" },
            { "ModifiedDate", "$docs.ModifiedDate" },
            { "Embedding", "$docs.Embedding" }
        }));

        // 6. Union with text search results
        var textSearchPipeline = new BsonArray
        {
            BuildTextSearchStage(userId, queryParameters),
            new BsonDocument("$limit", queryParameters.PageSize * 2),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "docs", new BsonDocument("$push", "$$ROOT") }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$docs" },
                { "includeArrayIndex", "rank" }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "ts_score", new BsonDocument("$divide", new BsonArray
                    {
                        1.0,
                        new BsonDocument("$add", new BsonArray { "$rank", textPriority, 1 })
                    })
                }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "ts_score", 1 },
                { "_id", "$docs._id" },
                { "Title", "$docs.Title" },
                { "Authors", "$docs.Authors" },
                { "Description", "$docs.Description" },
                { "Publisher", "$docs.Publisher" },
                { "Tags", "$docs.Tags" },
                { "Annotation", "$docs.Annotation" },
                { "Isbn10", "$docs.Isbn10" },
                { "Isbn13", "$docs.Isbn13" },
                { "Pages", "$docs.Pages" },
                { "CoverImageUrl", "$docs.CoverImageUrl" },
                { "PublicationDate", "$docs.PublicationDate" },
                { "UserId", "$docs.UserId" },
                { "Status", "$docs.Status" },
                { "StartedReadingDate", "$docs.StartedReadingDate" },
                { "FinishedReadingDate", "$docs.FinishedReadingDate" },
                { "CreatedDate", "$docs.CreatedDate" },
                { "ModifiedDate", "$docs.ModifiedDate" },
                { "Embedding", "$docs.Embedding" }
            })
        };

        pipeline.Add(new BsonDocument("$unionWith", new BsonDocument
        {
            { "coll", BookEntity.CollectionName },
            { "pipeline", textSearchPipeline }
        }));

        // 7. Combine scores from both searches (take max for each document)
        pipeline.Add(new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$_id" },
            { "vs_score", new BsonDocument("$max", "$vs_score") },
            { "ts_score", new BsonDocument("$max", "$ts_score") },
            { "Title", new BsonDocument("$first", "$Title") },
            { "Authors", new BsonDocument("$first", "$Authors") },
            { "Description", new BsonDocument("$first", "$Description") },
            { "Publisher", new BsonDocument("$first", "$Publisher") },
            { "Tags", new BsonDocument("$first", "$Tags") },
            { "Annotation", new BsonDocument("$first", "$Annotation") },
            { "Isbn10", new BsonDocument("$first", "$Isbn10") },
            { "Isbn13", new BsonDocument("$first", "$Isbn13") },
            { "Pages", new BsonDocument("$first", "$Pages") },
            { "CoverImageUrl", new BsonDocument("$first", "$CoverImageUrl") },
            { "PublicationDate", new BsonDocument("$first", "$PublicationDate") },
            { "UserId", new BsonDocument("$first", "$UserId") },
            { "Status", new BsonDocument("$first", "$Status") },
            { "StartedReadingDate", new BsonDocument("$first", "$StartedReadingDate") },
            { "FinishedReadingDate", new BsonDocument("$first", "$FinishedReadingDate") },
            { "CreatedDate", new BsonDocument("$first", "$CreatedDate") },
            { "ModifiedDate", new BsonDocument("$first", "$ModifiedDate") },
            { "Embedding", new BsonDocument("$first", "$Embedding") }
        }));

        // 8. Calculate combined RRF score
        pipeline.Add(new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "Title", 1 },
            { "Authors", 1 },
            { "Description", 1 },
            { "Publisher", 1 },
            { "Tags", 1 },
            { "Annotation", 1 },
            { "Isbn10", 1 },
            { "Isbn13", 1 },
            { "Pages", 1 },
            { "CoverImageUrl", 1 },
            { "PublicationDate", 1 },
            { "UserId", 1 },
            { "Status", 1 },
            { "StartedReadingDate", 1 },
            { "FinishedReadingDate", 1 },
            { "CreatedDate", 1 },
            { "ModifiedDate", 1 },
            { "Embedding", 1 },
            { "score", new BsonDocument("$let", new BsonDocument
                {
                    { "vars", new BsonDocument
                        {
                            { "vs_score", new BsonDocument("$ifNull", new BsonArray { "$vs_score", 0.0 }) },
                            { "ts_score", new BsonDocument("$ifNull", new BsonArray { "$ts_score", 0.0 }) }
                        }
                    },
                    { "in", new BsonDocument("$add", new BsonArray { "$$vs_score", "$$ts_score" }) }
                })
            }
        }));

        // 9. Sort by combined score (descending)
        pipeline.Add(new BsonDocument("$sort", new BsonDocument("score", -1)));

        // 10. Pagination
        pipeline.Add(new BsonDocument("$skip", (queryParameters.Page - 1) * queryParameters.PageSize));
        pipeline.Add(new BsonDocument("$limit", queryParameters.PageSize));

        // Execute the aggregation
        var books = await _booksCollection.Aggregate<BookEntity>(pipeline).ToListAsync();
        var mappedBooks = books.Select(mapper.Map).ToList();

        // Get total count (simplified - could be optimized)
        var totalCount = mappedBooks.Count; // For now, use result count. TODO: Add proper count pipeline

        return new PagedResult<Book>(
            mappedBooks,
            totalCount,
            queryParameters.Page,
            queryParameters.PageSize);
    }

    private async Task<TryResult<PagedResult<Book>>> SearchTextOnlyAsync(ObjectId userId, SearchQueryParameters queryParameters)
    {
        var scoreBuilder = Builders<BookEntity>.SearchScore;
        var searchBuilder = Builders<BookEntity>.Search;
        var looseFuzzy = new SearchFuzzyOptions { MaxEdits = 1, PrefixLength = 2 };

        var compoundBuilder = searchBuilder
            .Compound()
            .Filter(searchBuilder.Equals(f => f.UserId, userId));

        if (queryParameters.Status.HasValue)
        {
            compoundBuilder = compoundBuilder.Filter(searchBuilder.Equals(f => f.Status, queryParameters.Status.Value));
        }

        var searchFilter = compoundBuilder
            .Should(
                searchBuilder.Autocomplete(f => f.Title, queryParameters.SearchTerm, score: scoreBuilder.Boost(3.0)),
                searchBuilder.Text(f => f.Title, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)),
                searchBuilder.Text(f => f.Title, queryParameters.SearchTerm, fuzzy: looseFuzzy, score: scoreBuilder.Boost(2.0)),
                searchBuilder.Autocomplete(f => f.Authors, queryParameters.SearchTerm, score: scoreBuilder.Boost(3.0)),
                searchBuilder.Text(f => f.Authors, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)),
                searchBuilder.Text(f => f.Authors, queryParameters.SearchTerm, fuzzy: looseFuzzy, score: scoreBuilder.Boost(2.0)),
                searchBuilder.Autocomplete(f => f.Publisher, queryParameters.SearchTerm, score: scoreBuilder.Boost(3.0)),
                searchBuilder.Text(f => f.Publisher, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)),
                searchBuilder.Text(f => f.Publisher, queryParameters.SearchTerm, fuzzy: looseFuzzy, score: scoreBuilder.Boost(2.0)),
                searchBuilder.Text(f => f.Tags, queryParameters.SearchTerm, score: scoreBuilder.Boost(2.0)),
                searchBuilder.Text(f => f.Description, queryParameters.SearchTerm, score: scoreBuilder.Boost(1.0)),
                searchBuilder.Text(f => f.Annotation, queryParameters.SearchTerm, score: scoreBuilder.Boost(1.0))
            )
            .MinimumShouldMatch(1);

        var books = await _booksCollection
            .Aggregate()
            .Search(searchFilter)
            .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
            .Limit(queryParameters.PageSize)
            .ToListAsync();

        var totalCount = await _booksCollection
            .Aggregate()
            .Search(searchFilter)
            .Count()
            .FirstOrDefaultAsync();

        var mappedBooks = books.Select(mapper.Map).ToList();

        return new PagedResult<Book>(
            mappedBooks,
            totalCount?.Count ?? 0,
            queryParameters.Page,
            queryParameters.PageSize);
    }

    private static BsonDocument BuildFilterDocument(ObjectId userId, ReadingStatus? status)
    {
        var filters = new BsonArray
        {
            new BsonDocument("equals", new BsonDocument
            {
                { "path", "UserId" },
                { "value", userId }
            })
        };

        if (status.HasValue)
        {
            filters.Add(new BsonDocument("equals", new BsonDocument
            {
                { "path", "Status" },
                { "value", status.Value.ToString() }
            }));
        }

        return new BsonDocument("$and", filters);
    }

    private static BsonDocument BuildTextSearchStage(ObjectId userId, SearchQueryParameters queryParameters)
    {
        var compound = new BsonDocument
        {
            { "filter", new BsonArray
                {
                    new BsonDocument("equals", new BsonDocument
                    {
                        { "path", "UserId" },
                        { "value", userId }
                    })
                }
            },
            { "should", new BsonArray
                {
                    new BsonDocument("text", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Title" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 8.0)) }
                    }),
                    new BsonDocument("text", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Authors" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 8.0)) }
                    }),
                    new BsonDocument("autocomplete", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Title" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 3.0)) }
                    }),
                    new BsonDocument("autocomplete", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Authors" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 3.0)) }
                    }),
                    new BsonDocument("text", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Publisher" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 5.0)) }
                    }),
                    new BsonDocument("text", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Tags" },
                        { "score", new BsonDocument("boost", new BsonDocument("value", 2.0)) }
                    }),
                    new BsonDocument("text", new BsonDocument
                    {
                        { "query", queryParameters.SearchTerm },
                        { "path", "Description" }
                    })
                }
            },
            { "minimumShouldMatch", 1 }
        };

        if (queryParameters.Status.HasValue)
        {
            var filterArray = compound["filter"].AsBsonArray;
            filterArray.Add(new BsonDocument("equals", new BsonDocument
            {
                { "path", "Status" },
                { "value", queryParameters.Status.Value.ToString() }
            }));
        }

        return new BsonDocument("$search", new BsonDocument
        {
            { "index", "default" },
            { "compound", compound }
        });
    }
}