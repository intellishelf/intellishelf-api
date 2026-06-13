# Search Behavior

## Purpose

Search should help the user find the book they meant with the least possible thinking.

The guiding rule is:

> Find what the user clearly asked for first, then intelligently expand to what they probably meant.

Search is one user experience. The user should not choose between "text search", "autocomplete",
"fuzzy search", or "vector search". Those are implementation tools. The product should feel like
one search box that behaves well in different situations.

## Core Principles

1. Exact and obvious matches must win.
   - `Harry Potter` should put Harry Potter books first.
   - `Shakespeare` should prioritize books by or about Shakespeare.
   - `Dune` should not be buried below loosely related science-fiction books.

2. Typing should feel fast and forgiving.
   - `ha` can show likely title or author matches immediately.
   - Typos should still find reasonable matches when the intent is clear.
   - Autocomplete may be fuzzy, but it should stay lightweight.

3. Vague queries should become useful discovery.
   - `dark fantasy` should find books that are actually dark fantasy, gothic, grim, or adjacent.
   - `books about loneliness and isolation` should eventually benefit from semantic search.
   - Related results are welcome only after clear exact matches are protected.

4. Weird top results are worse than missing a few secondary results.
   - `dark fantasy` should not rank `Heart of Darkness` first just because it contains `dark`.
   - A famous author name should not return random books from the same broad theme above the author.

## Search Moments

### While Typing

Autocomplete is the while-typing experience.

Its job is to help the user finish the query or jump quickly to a likely book. It should be:

- fast enough to update as the user types,
- typo-tolerant,
- focused on high-signal fields,
- limited to a small result set,
- safe to call repeatedly with debounce from the client.

Good fields for autocomplete:

- title,
- authors,
- publisher,
- tags when they are useful and curated.

Fields that should be used carefully while typing:

- description,
- annotation.

Descriptions and annotations can be noisy for short inputs. They are more useful in submitted
search, where the query is complete and pagination is expected.

Autocomplete can show actual matching books immediately. It does not need to be only query text
suggestions. For IntelliShelf, actual book matches are usually more useful than generic query
suggestions because each user searches their own personal library.

### Submitted Search

Submitted search is the full result-list experience after the user confirms a query or lands on a
search results screen.

It should:

- return paginated books,
- include total count,
- support filters such as reading status,
- rank exact and strong lexical matches first,
- include fuzzy matches for typos,
- later include vector results for semantic discovery.

Submitted search can do more work than autocomplete because it is not called on every keystroke.
This is the right place for future query embeddings and hybrid ranking.

## Ranking Priority

The ranking philosophy is:

1. Exact or strong lexical match.
2. Partial, prefix, or autocomplete match.
3. Fuzzy lexical match.
4. Metadata match.
5. Semantic/vector match.

This is a product rule, not a strict formula. The important behavior is that semantic expansion
should enrich results without overriding obvious matches.

## Query Intent Examples

| Query | Expected behavior |
| --- | --- |
| `Harry Potter` | Title match dominates. Harry Potter books should be first. |
| `Shakespeare` | Author/title/metadata text matches dominate. |
| `Shakespare` | Fuzzy matching should recover Shakespeare results. |
| `Dune` | Exact title matches should outrank broad desert or sci-fi concepts. |
| `dark fantasy` | Tags, genre-like metadata, description, and later vectors help. |
| `books about loneliness and isolation` | Vector search should eventually matter more. |
| `ha` | Autocomplete/prefix behavior matters; vector search should not run. |

## Autocomplete Contract

Autocomplete is conceptually separate from full search, even if the first implementation reuses the
same backend endpoint.

Autocomplete should:

- return a small number of likely books, such as 5 to 10,
- tolerate simple typos,
- prioritize title and author,
- feel instant for small personal libraries,
- avoid expensive semantic/vector work,
- avoid returning every possible field or heavy payload if a separate endpoint is added later.

First implementation may use:

```text
GET /api/books/search?searchTerm={term}&pageSize=8
```

Future implementation may add a dedicated endpoint, for example:

```text
GET /api/books/search/suggestions?term={term}&limit=8
```

A dedicated endpoint becomes valuable when autocomplete needs a different response shape, no total
count, highlights, grouped suggestions, or different ranking from full search.

## Fuzzy Search Expectations

Fuzzy search can be fast when it is backed by a search index. It should not be understood as "scan
all books and compare strings". Systems such as Atlas Search, Elasticsearch/OpenSearch, Algolia, and
commercial e-commerce search plugins use prebuilt indexes to make typo-tolerant search feel instant.

For IntelliShelf:

- fuzzy matching is useful both while typing and after submit,
- fuzzy matching should use conservative typo tolerance at first,
- fuzzy matches should not outrank exact matches,
- fuzzy behavior should focus on title, author, and publisher before long text fields.

## Vector Search Expectations

Vector search is part of the full search vision, but it is not required for the first iteration.

Vector search should help with:

- natural language queries,
- mood or theme queries,
- conceptual discovery,
- "similar books" features later.

Vector search should not:

- run for every keystroke,
- replace lexical search,
- outrank exact title or author matches,
- become the only ranking signal.

The best product behavior is hybrid search: lexical search provides precision, vector search adds
recall and discovery.

## Bad Results To Avoid

- A proper noun query returns thematic neighbors above exact matches.
- A short prefix query returns semantic guesses instead of obvious title/author prefixes.
- A common word in a title beats the user's intended genre or theme.
- Long descriptions dominate ranking because they contain many matching terms.
- The user has to choose a mode before searching.

## Success Criteria

Search feels good when:

- users can find known books by title or author quickly,
- typo tolerance fixes ordinary mistakes,
- autocomplete helps without feeling noisy,
- vague discovery queries produce plausible results,
- exact matches stay protected after semantic search is added,
- behavior is explainable enough to tune without guessing.
