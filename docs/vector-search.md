# Vector Search Architecture & Implementation Plan

## Business Validation

Vector Search is an incredibly powerful feature for a library/book application like Intellishelf. Here is why it's worth implementing:

1. **Semantic Matching ("Vibe" Search)**: Basic lexical search only finds exact matches or simple variations (e.g., "science" matches "science"). Vector search understands meaning. If a user searches for "Shakespare", they won't just find books with the typo, but also works by *William Shakespeare*, books about *Macbeth*, *tragedy plays*, or *Elizabethan literature*.
2. **Discovery & Natural Language Queries**: Users can search for concepts like "a book about a boy going to a wizard school" and immediately get *Harry Potter*, even if those exact words aren't in the book's description.
3. **Recommendation Foundation (Related Books)**: Once every book has a vector embedding, finding "similar books" becomes a cheap and native query. You simply do a `$vectorSearch` using the current book's vector as the query vector.
4. **Resilience to Typos**: Vector embeddings cluster similar concepts together across typos and synonyms seamlessly.

**How to Combine & Rank (Hybrid Search)**: 
The industry standard is **Hybrid Search**, which combines traditional keyword search with vector search using a technique called **Reciprocal Rank Fusion (RRF)**. MongoDB Atlas natively supports this, allowing you to run your existing `$search` and a new `$vectorSearch` simultaneously, merging the results for the best of both worlds.

---

## Technical Implementation with MongoDB Atlas (.NET 9 + Mongo 3.2.1)

Since you are already using `MongoDB.Driver` version `3.2.1` and MongoDB Atlas Search, implementing this is very straightforward because the latest driver fully supports `$vectorSearch`. 

Here is the step-by-step implementation plan:

### 1. Update Database Entities
We need to store the vector representations of the books. We'll use a `float[]` array for this.

#### `Intellishelf.Data/Books/Entities/BookEntity.cs`
Add the `Embedding` field:
```csharp
public float[]? Embedding { get; init; }
```

---

### 2. Generate Embeddings (OpenAI)
You already have `Ai__OpenAiApiKey` in your `docker-compose.yml`. We need a service to call OpenAI's embedding API (e.g., `text-embedding-3-small`) when a book is added or updated.

#### `Intellishelf.Domain/Ai/Services/IEmbeddingService.cs`
```csharp
public interface IEmbeddingService
{
    Task<TryResult<float[]>> GenerateEmbeddingAsync(string text);
}
```

#### `Intellishelf.Domain/Books/Services/BookService.cs`
Update `TryAddBookAsync` and `TryUpdateBookAsync` to concatenate relevant book fields (Title + Authors + Description) and generate an embedding before saving the book to the database.

---

### 3. Update the Search Logic (Hybrid Search)
Your existing `SearchAsync` uses `Builders<BookEntity>.Search`. We will upgrade this to a Hybrid Search query using both Keyword Search and Vector Search with Reciprocal Rank Fusion (RRF).

#### `Intellishelf.Data/Books/DataAccess/BookDao.cs`

Update the `SearchAsync` method. Since you are passing `SearchQueryParameters`, you'll first generate an embedding for `queryParameters.SearchTerm` and then execute the combined query.
*(Note: As discovered during implementation, Mongo driver 3.2 lacks extension methods for vector search so we execute a raw `$vectorSearch` BsonDocument stage before falling back to `$search`)*

```csharp
// Example using raw BSON for $vectorSearch:
var vectorSearchDoc = new BsonDocument("$vectorSearch", new BsonDocument
{
    { "index", "vector_index" },
    { "path", "Embedding" },
    { "queryVector", new BsonArray(vectorOption.Select(v => (BsonValue)v)) },
    { "numCandidates", 100 },
    { "limit", limit },
    { "filter", filter }
});
```

---

### 4. Setup MongoDB Atlas Vector Index

We will configure the MongoDB C# driver to programmatically ensure that the Atlas Vector Search Index is created on application startup. This is much easier to test, automate, and ensures schema consistency.

#### `Intellishelf.Api/Modules/DatabaseInitializer.cs` 
We will add a method that connects to your MongoDB Atlas cluster using your connection string and creates the index using `MongoDB.Driver` 3.2.1:

```csharp
public static async Task EnsureVectorIndex(IMongoDatabase database)
{
    var collection = database.GetCollection<BookEntity>(BookEntity.CollectionName);
    
    // Why these fields?
    // 1. the "vector" type is mapped to the 'Embedding' property, generated as strictly 1536 floats by OpenAI's 'text-embedding-3-small'.
    // 2. The 'cosine' similarity calculates similarity based on angles, which is strictly how OpenAI trains its embeddings.
    // 3. The "filter" types for 'UserId' and 'Status' instruct Atlas to PRE-FILTER results during the actual math phase. 
    //    Pre-filtering avoids massive performance hits and ensures you never get book vectors from the wrong multi-tenant user.
    var indexDefinition = new BsonDocument
    {
        { "fields", new BsonArray
            {
                new BsonDocument { { "type", "vector" }, { "path", "Embedding" }, { "numDimensions", 1536 }, { "similarity", "cosine" } },
                new BsonDocument { { "type", "filter" }, { "path", "UserId" } },
                new BsonDocument { { "type", "filter" }, { "path", "Status" } }
            }
        }
    };

    var model = new CreateSearchIndexModel("vector_index", indexDefinition);

    try
    {
        var indexes = await (await collection.SearchIndexes.ListAsync()).ToListAsync();
        if (!indexes.Any(i => i["name"] == "vector_index"))
        {
            await collection.SearchIndexes.CreateOneAsync(model);
        }
    }
    catch (Exception ex) // Catch duplicate index exceptions or Atlas specific issues
    {
        // Log "vector_index already exists or creation failed"
    }
}
```
*Note: We will call this initializer during your `Program.cs` startup pipeline.*
