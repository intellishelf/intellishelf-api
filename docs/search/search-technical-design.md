# Search Technical Design

## Purpose

This document is the canonical technical design for IntelliShelf book search. It replaces the older
search notes and captures both the current implementation and the intended target architecture.

The product behavior source of truth is `docs/search/search-behavior.md`.

## Current Implementation

The current API exposes full search through:

```text
GET /api/books/search
```

The endpoint is implemented by:

- `src/Intellishelf.Api/Controllers/BooksController.cs`
- `src/Intellishelf.Domain/Books/Services/BookService.cs`
- `src/Intellishelf.Data/Books/DataAccess/BookDao.cs`
- `src/Intellishelf.Domain/Books/Models/SearchQueryParameters.cs`

The request query model is:

```csharp
public class SearchQueryParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;
    private int _pageSize = DefaultPageSize;

    public required string SearchTerm { get; init; }
    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public ReadingStatus? Status { get; init; }
}
```

`BookDao.SearchAsync` uses MongoDB Atlas Search with a compound query:

- `filter` by `UserId`,
- optional `filter` by `Status`,
- `should` clauses for lexical, autocomplete, and fuzzy matches,
- `MinimumShouldMatch(1)`,
- pagination with `Skip` and `Limit`,
- a separate `Count()` aggregation for `TotalCount`.

Current searched fields and boosts:

| Field | Operators | Boosts |
| --- | --- | --- |
| `Title` | autocomplete, text, fuzzy text | 3.0, 8.0, 2.0 |
| `Authors` | autocomplete, text, fuzzy text | 3.0, 8.0, 2.0 |
| `Publisher` | autocomplete, text, fuzzy text | 3.0, 8.0, 2.0 |
| `Tags` | text | 2.0 |
| `Description` | text | 1.0 |
| `Annotation` | text | 1.0 |

Current fuzzy options:

```csharp
new SearchFuzzyOptions
{
    MaxEdits = 1,
    PrefixLength = 2
}
```

This is a good conservative default. It catches simple typos while avoiding overly broad matches.

## Current Atlas Search Index

The integration test fixture index is:

```text
tests/Intellishelf.Integration.Tests/Infra/Fixtures/search-index.json
```

The setup script for real Atlas environments is:

```text
docs/search/setup-hybrid-search-indexes.js
```

Keep the JavaScript setup script. It is useful as the practical, executable reference for index
creation and updates.

The lexical Atlas Search index should include:

- `Title` as `string` and `autocomplete`,
- `Authors` as `string` and `autocomplete`,
- `Publisher` as `string` and `autocomplete`,
- `Tags` as `string`,
- `Description` as `string`,
- `Annotation` as `string`,
- `UserId` as `objectId`,
- `Status` as the serialized enum type.

Today `ReadingStatus` is a C# enum without custom string serialization, so Atlas Search setup should
treat `Status` as numeric unless serialization changes.

The test fixture currently focuses on the fields needed by existing search tests. If status-filtered
search gets integration coverage, the fixture should include `Status` too.

## First Iteration Target

The first polished version should keep Atlas Search as the only search engine.

Goals:

- preserve the existing `/api/books/search` endpoint,
- tune lexical, autocomplete, and fuzzy ranking,
- document boost priorities,
- make autocomplete feel good without a separate backend if possible,
- avoid vector search on every keystroke,
- add tests for ranking behavior that matters to the product.

Autocomplete is a separate product contract, but it can initially call:

```text
GET /api/books/search?searchTerm={term}&pageSize=8
```

This is enough until autocomplete needs one or more of:

- a smaller response DTO,
- no total count,
- highlights,
- grouped suggestions,
- different boosts from full search,
- no description/annotation matching,
- stricter latency controls.

At that point, add a dedicated endpoint such as:

```text
GET /api/books/search/suggestions?term={term}&limit=8
```

The dedicated endpoint should still reuse shared search-building logic where possible so ranking
rules do not drift accidentally.

## Future Vector Search Target

Vector search is part of the overall search design, but not the first implementation milestone.

The future vector layer should support:

- semantic full-query search,
- natural language discovery,
- theme and mood queries,
- later "similar books" features.

It should not be used for every autocomplete keystroke.

### Book Embeddings

Add an embedding field to `BookEntity` only when vector search implementation starts:

```csharp
public float[]? Embedding { get; init; }
```

The embedding text should be built from high-signal book data, for example:

- title,
- authors,
- tags,
- description,
- annotation,
- publisher if useful.

Keep the embedding input deterministic and documented. If the embedding text format changes, existing
books may need re-embedding.

Embedding dimensions must match the selected embedding model. Older notes mentioned both 1536 and
3072 dimensions. Treat those as stale examples until the model is chosen. At implementation time,
verify the current OpenAI model dimensions and set the Atlas Vector Search index accordingly.

### Vector Index

MongoDB Atlas uses a separate Vector Search index for `$vectorSearch`.

The setup script already includes the target shape:

```javascript
const vectorDefinition = {
  fields: [
    { type: "vector", path: "Embedding", numDimensions: 1536, similarity: "cosine" },
    { type: "filter", path: "UserId" },
    { type: "filter", path: "Status" }
  ]
};
```

Before enabling this in production, update `numDimensions` to match the selected embedding model.

Vector search must filter by:

- `UserId`,
- optional `Status`.

This preserves user isolation and avoids expensive post-filtering.

## Hybrid Ranking

The target architecture is hybrid search:

1. Run lexical Atlas Search.
2. Run vector search for submitted queries that are long or semantic enough to benefit.
3. Merge the ranked lists.
4. Deduplicate by book id.
5. Return paginated results.

Prefer Reciprocal Rank Fusion (RRF) or an RRF-style approach when the vector phase is added.

RRF is preferable to raw score addition because lexical scores and vector similarity scores are not
naturally comparable. Rank fusion is easier to reason about and tune:

```text
score = lexicalWeight / (k + lexicalRank) + vectorWeight / (k + vectorRank)
```

Product constraints for hybrid ranking:

- exact title and author matches must remain protected,
- short proper-noun queries should be mostly lexical,
- longer natural-language queries may allow vector results to contribute more,
- vector results should enrich, not replace, lexical relevance.

## Query Intent Heuristics

Do not expose search modes to the user. Use query shape internally.

Initial heuristics:

- very short queries: lexical/autocomplete only,
- title-like or author-like queries: lexical-dominant,
- queries with multiple descriptive words: allow semantic contribution,
- long natural-language queries: vector can contribute more,
- empty or whitespace-only queries: reject or return an empty result according to API validation.

These heuristics should stay simple until real usage shows a need for more.

## Error Handling

Search should follow the repository's `TryResult` pattern.

Expected behavior:

- invalid query input returns a domain error mapped by `ApiControllerBase`,
- search infrastructure failures return an error rather than leaking driver exceptions,
- vector search failures should not silently corrupt ranking,
- if vector search is optional in a future phase, decide explicitly whether lexical fallback is
  acceptable and log the degraded path.

Current search code does not add custom search error codes. Add them only when the service starts
handling validation or external vector/embedding failures explicitly.

## Testing Expectations

Existing integration tests cover:

- empty results,
- title search,
- author search,
- multiple matches,
- pagination.

Important future tests:

- exact title outranks fuzzy or partial matches,
- author query returns that author's books,
- typo query recovers expected book or author,
- status filter works with Atlas Search index,
- short autocomplete-style query returns plausible prefix matches,
- description/annotation matches do not outrank exact title/author matches,
- later: vector search helps conceptual queries,
- later: vector search does not outrank exact title/author matches.

For integration tests, remember that Atlas Search indexing is asynchronous. Use the existing
`SeedBooksAndWaitForIndexing` fixture path for search tests.

## Deployment Notes

Atlas Search indexes are not ordinary MongoDB indexes. They must exist before search behaves
correctly in Atlas environments.

Use:

```text
docs/search/setup-hybrid-search-indexes.js
```

Example:

```bash
mongosh "<connection-string>/<database-name>" docs/search/setup-hybrid-search-indexes.js
```

The script creates or updates:

- `default` Atlas Search index,
- `vector_index` Atlas Vector Search index.

For the first iteration, only the lexical `default` index is required by application code. The vector
index can exist ahead of time, but application code should not depend on it until embeddings and
hybrid search are implemented.

Index creation can take several minutes. Verify index readiness in Atlas before judging search
quality.

## Implementation Stages

### Stage 1: Current Lexical Search

Status: implemented.

- Atlas Search text/autocomplete/fuzzy search,
- user filter,
- optional status filter in query,
- pagination and total count.

### Stage 2: Polish Lexical and Autocomplete Behavior

Status: next practical milestone.

- document and tune boosts,
- decide whether to add a dedicated autocomplete endpoint,
- add ranking-focused tests,
- ensure index definitions are consistent across test fixture and setup script.

### Stage 3: Embeddings

Status: future.

- choose embedding model,
- add `Embedding` to `BookEntity`,
- generate embeddings on add/update,
- backfill existing books,
- verify dimensions in `vector_index`.

### Stage 4: Hybrid Search

Status: future.

- generate query embedding for submitted searches,
- run lexical and vector retrieval,
- merge with RRF-style ranking,
- keep exact/proper-noun matches protected,
- add semantic quality tests.

### Stage 5: Similar Books

Status: optional future feature.

- use a book's stored embedding as the query vector,
- filter by user,
- exclude the source book,
- return related books as a separate feature from search.

## Non-Goals

- No user-visible search mode selector.
- No vector search during every keystroke.
- No external hosted search product unless Atlas Search becomes insufficient.
- No complex query language for the user.
- No popularity ranking until the app has meaningful usage data.
