# Search Feature Plan

## Goal

"Find what the user clearly asked for first, then intelligently expand to what they probably meant."

## What good search should feel like

1. **Specific queries -> exact matches first**
   - "Shakespeare" -> Shakespeare books, not "random drama"
   - "Harry Potter" -> exact title at the top

2. **Vague queries -> smart, relevant suggestions**
   - "dark novels" -> gothic, dystopian, grim books
   - not just anything containing the word "dark"

3. **Typing should feel responsive**
   - "ha" -> autocomplete suggestions instantly

4. **No weird irrelevant results at the top**
   - "dark fantasy" should NOT show "Heart of Darkness" at #1

## Ranking Priority

**Exact > Partial > Semantic**

1. Exact / strong keyword matches
2. Partial / prefix matches (autocomplete)
3. Semantic (vector) matches

This alone solves 80% of search quality.

## Query Intent (automatic, no user toggle)

| Query | Behavior |
|-------|----------|
| "Shakespeare" | text dominates |
| "Harry Potter" | exact match dominates |
| "dark novels" | mix, vector helps |
| "books about loneliness and isolation" | vector dominates |

- Short / clear queries -> text dominates
- Longer / vague queries -> semantic matters more
- No "search mode" selector for the user

## Autocomplete

- Help users type
- Should NOT dominate final ranking results

## Semantic (Vector) Search

- Bring related books as enrichment
- Never override obvious exact matches
- Think: "Expand results" not "replace logic"

## Metadata matters

Don't rely only on embeddings. Use:
- genres (Fantasy, Drama)
- tags (dark, gothic)
- author
- maybe year / popularity

This prevents: "dark fantasy" -> "Heart of Darkness" problem.

## Implementation Stages

### Stage 1 -- Text search
- Title, Author, Tags
- Strong weight

### Stage 2 -- Autocomplete
- Lower weight

### Stage 3 -- Vector search
- Medium / adaptive weight

### Stage 4 -- Filter
- userId, isDeleted, etc.

### Stage 5 -- Combine results
- Simple (Mongo scoring) or RRF (later)

## What to avoid

- Pure text search -> misses semantic queries
- Pure vector search -> ignores exact matches
- Equal weighting -> messy ranking
- Searching all fields blindly -> noisy results
- Asking user to choose "search mode" -> bad UX

## Current State

### Already implemented in code
- Text search on Title, Authors, Publisher (boost 8.0)
- Autocomplete on Title, Authors, Publisher (boost 3.0)
- Fuzzy search with 1 edit distance (boost 2.0)
- Tags, Description, Annotation text search (boost 1.0-2.0)
- userId filter via compound.filter
- Optional Status filter
- Pagination (skip/limit)
- Search index JSON definition (in test fixtures)

### Not yet implemented
- Atlas Search index on production database (index definition exists but not deployed)
- Vector/embedding field on BookEntity
- Vector search ($vectorSearch stage)
- RRF fusion (two-stage: $vectorSearch + $search + $unionWith)

## Future: RRF Hybrid Search

When ready for more control over text vs vector ranking:
- Two separate indexes: text index + vector index
- Two separate queries: $search (text) + $vectorSearch (vector)
- Fusion via $unionWith + RRF scoring: `score = 1/(k + rank_text) + 1/(k + rank_vector)`
- Allows explicit weight tuning between text and vector results

Not needed now. Upgrade path when:
- Need better control over text vs vector score weighting
- Want industry-standard RRF algorithm
- Need fine-tuned search relevance
