# Search and AI Library Discovery — Product & Technical Design

**Status:** Proposed product direction; existing lexical search is implemented, AI discovery is not.
**Scope:** Personal bookshelf search, Pro semantic discovery, embedding lifecycle, bulk backfill, metadata enrichment, and validation.
**Supersedes:** `search-behavior.md` and `search-technical-design.md`; useful examples and current implementation details are consolidated below.

## 1. Product decision

Intellishelf should have **two understandable user journeys**, without exposing technical algorithm switches:

- **Find my books (Free/default):** instant search for a known book by title, author, publisher, ISBN, tags or text; autocomplete and conservative typo tolerance. Results are actionable as the user types.
- **Explore with AI (Pro):** a visible secondary action **below the normal search results** opens a distinct discovery experience. Queries can describe themes, ideas, moods, or related books. Initially it returns a book list; a generative assistant is optional later.

This **replaces the older assumption** that all completed search queries must automatically run hybrid lexical/vector ranking. The default journey remains lexical. The Pro journey runs **hybrid retrieval by default**: lexical/fuzzy and vector search are both executed for a submitted Explore query, then fused and reranked. No query-intent classifier is required to decide whether to run vectors. Exceptions such as embedding-service outages are explicit degraded/fallback behavior, not routine routing decisions.

Guiding principles:
1. **Find what the user clearly asked for first; then expand to plausible alternatives.**
2. Exact title/author matches must never be buried under fuzzy or thematic guesses.
3. Never unexpectedly reshuffle a list while the user is selecting a book.
4. A small collection may have **no relevant books**, even if nearest-neighbor search always returns something.
5. Search stays scoped to the authenticated owner's books.
6. Semantic discovery is a paid *capability*, not a reason to degrade the Free product.

## 2. User flows

### 2.1 Find a known book
1. Open search, start typing `The Beg...` or `Dost...`.
2. Debounced lexical/autocomplete search returns ~5–10 clickable books, prioritizing title/author.
3. Tap the intended book and go straight to its detail page; **do not** invoke embeddings.
4. Optionally submit to view stable paginated lexical results, filters, and total count.

Typing should favor high-signal fields (title/author, publisher if useful) rather than noisy long descriptions. No vector search on every keystroke. The existing search endpoint can serve the MVP; a lean suggestions route is optional.

### 2.2 Explore with AI
1. Click **Explore with AI — Find books by ideas, themes and meaning** below the ordinary search.
2. Pro users enter a separate view with a larger prompt and examples such as:
   - "Books about how knowledge grows"
   - "Stories of loneliness and isolation"
   - "What do I own about scientific explanations?"
   - "Books similar to The Beginning of Infinity"
3. On submit, run **both** user-scoped lexical/fuzzy retrieval and vector retrieval (after obtaining the query embedding), then fuse/rerank them into one stable list.
4. Show **one stable final result list**; do not first paint lexical results and later reorder them when vector retrieval completes.
5. Selecting a book opens its normal detail page. Free users see an accurate Pro explanation/upgrade affordance instead of a fake AI result.

This is a product-level distinction (find vs explore), **not** an exposed `Lexical / Vector` toggle.

### 2.3 No obvious matches
If normal search finds none, offer **Try exploring by meaning** with the original input. If semantic candidates do not pass a calibrated relevance check, prefer an honest "No relevant books in your library" over arbitrary nearest neighbors.

### 2.4 Preparing an upgraded library
When a Free account with many books upgrades to Pro, show **Preparing your library** with total/ready/failed counts and ordinary bookshelf features fully usable. First recommendation: unlock complete-library discovery when backfill is complete, rather than returning silently incomplete search results. An explicitly marked partial-results mode can be tested later. Define what "complete" means for books that cannot be enriched or indexed.

### 2.5 Errors, latency and fallbacks
- Pro discovery may show a loading state while the embedding provider and retrieval work.
- On provider failure, return an explicit **final** lexical fallback only if chosen by product policy, or display retryable error; never secretly reorder visible results later.
- Do not infer instant semantic response from small per-user vector sets: external query-embedding latency can dominate.
- Consider local embeddings or ENN for small collections only after benchmarks.

## 3. Current implementation (code-grounded)

`GET /api/books/search` exists in `BooksController`, delegates through `BookService` to `BookDao.SearchAsync`. The DAO uses MongoDB Atlas Search `$search` with:
- compound `filter` on `UserId`, optional `Status`;
- `should` clauses for title/author/publisher autocomplete (boost 3), ordinary text (boost 8), fuzzy text (boost 2);
- text on tags (boost 2), description/annotation (boost 1);
- fuzzy `MaxEdits = 1`, `PrefixLength = 2`;
- `MinimumShouldMatch(1)`, skip/limit pagination and a separate count aggregation.

`MongoIndexSetup.EnsureAsync` runs on API startup and in the integration fixture. It creates missing lexical and vector indexes and waits for them to become ready. Existing indexes are not compared with or updated to match the code; update them separately when their definitions change. The fixture uses **`mongodb/mongodb-atlas-local:latest`** in Testcontainers. Search seeding currently uses a fixed indexing delay.

`BookEntity` has an optional `Embedding` field, and `BookSearchIndexes` defines a 1536-dimensional `vector_index`. The vector index does not make vector retrieval live, and 1536 dimensions are not a final model selection. An integration test verifies vector queries against synthetic vectors in MongoDB.

**Not implemented:** generating or populating book embeddings, query embeddings, `$vectorSearch` from app code, fusion, semantic/Pro discovery API, embedding jobs and backfill.

## 4. Ranking and quality contract

Expected behavior examples (preserved from earlier search specs):

| Query | Default Find | Pro Explore (if invoked) |
| --- | --- | --- |
| `Harry Potter` | Exact title/books first | Protect exact match if hybrid retrieval is used |
| `Dune` | Exact book above loosely related science fiction | May additionally suggest thematic neighbors |
| `Shakespeare` | Author/title signal dominates | Can explore related literature without burying known author matches |
| `Shakespare` | Fuzzy recovery of author | Hybrid still runs both lexical and vector; fuzzy author result must outrank thematic neighbors |
| `ha` | Prefix autocomplete, fast | Do not embed every keystroke |
| `dark fantasy` | Tags/description if present; avoid accidental title-word dominance | Theme/genre discovery |
| `books about loneliness and isolation` | May find little lexical overlap | Conceptual discovery |
| `books about quantum physics` with none in library | Honest empty state | Do not call unrelated books "relevant" simply because nearest |

For default search, prioritize: exact/strong title-author, partial/prefix, typo-tolerant, then metadata matches. Treat these as product acceptance criteria, not proof that fixed numerical boosts guarantee that order.

For Pro discovery, **always retrieve lexical and vector candidates** on a normal completed query, then merge them via a tuned hybrid strategy (initial candidate: weighted RRF plus explicit exact-title/author protection). Evaluate ranking weights and alternatives, not whether to switch either retrieval engine off per query. RRF merges rankings, not incompatible raw scores, and **does not automatically guarantee exact-match dominance**. Use tests and explicit policy.

## 5. Embedding lifecycle

### 5.1 Generate asynchronously when content changes

1. Save the book immediately (book creation/edit must not wait on an AI request).
2. Construct a **deterministic** embedding input from selected metadata (title, authors, description, annotation, tags, optionally publisher).
3. Hash normalized embedding input and store model/schema version.
4. If that content hash + version is already indexed, do not regenerate. Reading status, cover, reading dates etc. should not trigger embeddings if their content is excluded.
5. Enqueue durable, idempotent work; worker generates embedding, checks dimensions and the still-current hash/version, then writes only if still valid. Concurrent edits must not be overwritten by stale vectors.
6. Track `NotIndexed -> Pending -> Processing -> Ready`, with retryable `Failed`, bounded backoff and operational error metrics.

Possible logical fields (not a final schema): `Embedding`, `EmbeddingModel`, `EmbeddingSchemaVersion`, `EmbeddingInputHash`, `EmbeddingStatus`, last error/time. Job records and aggregate backfill status may live separately. On deletion, clean up vectors, derived data and queued work.

### 5.2 Free -> Pro initial backfill

**Recommendation:** the upgrade triggers *immediate background backfill*, not a scheduled overnight-only refresh. Split a 1,000-book library into bounded batches/jobs, rate-limit concurrency, checkpoint progress and resume on crashes. Deduplicate jobs and check eligibility/ownership at execution time.

**Billing:** include initial indexing and normal edit-driven reindexing in Pro. **Do not deduct 1,000 AI chat credits** because 1,000 documents need embeddings. Embeddings are an internal index, and future model migrations/re-embedding should not become unexpected user charges. Meter actual provider tokens, retries, storage and index costs. Set fair-use limits to prevent extreme churn or unexpectedly huge collections.

On downgrade, stop new Pro-only jobs when entitlement expires; do not immediately discard still-valid vectors merely because of a downgrade, subject to a documented privacy/retention policy. On re-upgrade, reuse matching version/hash vectors.

Keep **entitlement**, **embedding readiness** and **embedding model/version** separate.

### 5.3 Small-library economics

For illustration only, 1,000 books × 300 embedding-input tokens = 300,000 input tokens. At a **hypothetical** $0.02 per million tokens, one backfill would cost $0.006 in embedding API charges. This is **not a verified current quote nor total cost**: MongoDB indexes/storage, metadata enrichment, outbound requests, worker operations and retries may be more important. Price and model must be verified before launch.

MongoDB Vector Search is a search capability backed by Lucene's vector machinery, **not** a second LLM. Embedding generation uses a model; vector-distance computation does not need a generative LLM.

## 6. Metadata enrichment: sourcing descriptions before embedding (tentative)

An important option is to **retrieve book descriptions from external catalogs before indexing**. This can make AI discovery substantially better when the user has only title/author/ISBN. It is a separate workflow and cost/privacy decision from embeddings.

Candidate sources to evaluate:
- **Google Books API**: structured bibliographic metadata and descriptions when available, subject to provider API terms, quotas and attribution requirements.
- **Open Library** and other licensed catalog/metadata providers: check per-record licensing, quality and API limits.
- **Perplexity/web discovery**: potentially useful as a controlled fallback to locate a source, but **not** a license to copy arbitrary publisher/reviewer descriptions. Search results or LLM-written summaries can be inaccurate and require verification.
- User-entered descriptions/annotations should remain authoritative and must never be silently overwritten.

Proposed enrichment pipeline:
1. On add/import or Pro backfill, check metadata completeness; prefer ISBN for identifying an exact edition.
2. Look up structured sources; match ISBN/edition, then verify title/author if fallback matching.
3. Record **provenance** (source, retrieved time, source identifier/url, license/attribution), confidence and original vs enriched field. Keep user-authored metadata distinct.
4. Apply source terms: **access does not imply permission to store, display, redistribute, or embed full third-party descriptions**. Avoid scraping and long copied text where rights are unclear; use appropriately licensed data or user-provided text.
5. Handle wrong editions, translations, conflicting text, missing sources, hallucinations and opt-outs; offer a review/accept mechanism if enrichment will modify user-visible book metadata.
6. Generate the embedding from an approved description/summary plus existing high-signal fields; track content fingerprint to avoid duplicate API calls.

**Open questions:** do we enrich Free book catalog metadata for everyone, or only Pro search? Does retrieval occur at book import, first Pro backfill or on demand? Are descriptions visible to the user or used only in a derived private index? What licensing/retention rules permit each? What is the cost of retrieval vs embedding?

Do **not** silently claim Google Search results are free-to-reuse descriptions. Consider separate "book facts" (title, author, ISBN) and protected descriptive prose.

## 7. Vector search and optional hybrid implementation

A future user-scoped MongoDB aggregate (illustrative only):

```javascript
db.Books.aggregate([
  {
    $vectorSearch: {
      index: "vector_index",
      path: "Embedding",
      queryVector: queryEmbedding,
      filter: {
        UserId: authenticatedUserObjectId
        // Status: selectedStatus, if requested
      },
      numCandidates: 200,
      limit: 10
    }
  },
  {
    $project: {
      Title: 1, Authors: 1,
      score: { $meta: "vectorSearchScore" }
    }
  }
]);
```

- `$search` indexes lexical/autocomplete/fuzzy data. `$vectorSearch` queries a separate vector index definition, under Atlas Search infrastructure.
- ENN (`exact: true`) can be benchmarked for small filtered libraries; ANN uses a neighbor graph and `numCandidates`. Avoid asserting a universal fastest strategy.
- Query vectors must have compatible model and dimensions with stored book vectors and index configuration.
- Vector similarity is **not a probability** that the book answers the user.
- `UserId` filter **must be inside** vector retrieval. Global Top-K then `$match` on user is not equivalent and can falsely return empty/partial results.
- If combining lexical and semantic candidates, deduplicate, protect exact matches, choose stable tie-breaking and define cursor/pagination/total-count semantics carefully. A bounded top-K is not an authoritative total count of all semantically relevant books.

## 8. Possible API contracts (all tentative)

- Keep **`GET /api/books/search`** as the standard lexical search.
- Consider `GET /api/books/search/suggestions?term=...&limit=8` when autocomplete needs a small DTO, separate boosts or no count query.
- Consider **`POST /api/books/discover`** for Pro natural-language discovery, user auth, entitlement checks and optional reading-status filter.
- Consider `GET /api/books/discovery-index/status` for Pro backfill status/progress.
- Responses should communicate final/partial/degraded state, and should **not** display raw vector scores as "percent relevant".

Implement with existing .NET Clean Architecture, `TryResult`, authenticated `CurrentUserId`, Domain abstractions, Data persistence and dependency-injection conventions.

## 9. Testing and release plan

1. **Strengthen existing lexical behavior:** exact title/author over fuzzy/description, typo tolerance, prefix queries, cross-user isolation, pagination, reading-status filtering. The shared test index includes `Status`; verify that deployed indexes match the application definition.
2. Build a realistic labeled corpus with hundreds/thousands of books and sparse metadata. Measure Top-3/Top-5 relevance across named books, typos, themes, similar books, and "no answer" cases.
3. Evaluate approved description-enrichment sources, edition matching, provenance and licensing **before** relying on enriched descriptions.
4. Benchmark whole-pipeline p50/p95 latency and cost: query embeddings, MongoDB ENN/ANN, optional fusion; do not infer total latency from the size of the per-user vector set.
5. Add durable backfill jobs, retries, version-safe writes, progress, entitlement gates and account/book deletion handling.
6. Beta **Explore with AI** with honest index-preparation/error/empty states. Observe discovery quality, search-to-book-open rate and failure modes.
7. Implement and evaluate **hybrid ranking by default for submitted Pro discovery**; tune its weights and exact-match protections. Avoid semantic work in default keystroke search.
8. Later: similar-books recommendations (reuse stored book vectors without a query embedding) or conversational/explanatory responses as distinct features.

No runtime changes are proposed by this documentation PR.

## 10. Unresolved product and engineering decisions

- What is the best Explore with AI surface (modal, page, panel) and CTA placement/copy?
- Should a partially indexed library be searchable with an explicit completeness notice?
- Which embedding model, dimensions and version/migration approach?
- Is title + author + user metadata sufficient, or should approved external descriptions be fetched first?
- Which metadata providers have usable licensing/attribution/retention rights?
- How should Pro and any future chat credits differ? What are fair-use and cancellation/retention policies?
- When does semantic search return "no relevant books", and how is that measured?
- ANN vs ENN for small personal libraries; which hybrid fusion/reranking formula and weights give the best Pro Explore results?
- How to paginate stable results and define meaningful counts for semantic Top-K?
- How to avoid unexpected metadata replacement, duplicate jobs and stale embeddings during edits/backfills?

**Decision boundary:** user flows and first-pass billing principle are proposed; routes, data contracts, model/provider, indexing strategy, enrichment policy and operational thresholds remain tentative.
