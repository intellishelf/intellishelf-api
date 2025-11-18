# MongoDB Atlas Index Configuration

This document describes the required MongoDB Atlas index for the IntelliShelf API search functionality.

## Required Index

The application uses **hybrid search** combining:
- **Text search** (exact matches, autocomplete, fuzzy matching)
- **Vector search** (semantic similarity via embeddings)

Both are handled by a **single Atlas Search index** with `knnVector` support.

---

## Atlas Search Index (Hybrid Text + Vector)

**Index Name:** `default`
**Index Type:** Atlas Search Index
**Collection:** `Books`

### Configuration JSON:

```json
{
  "mappings": {
    "dynamic": false,
    "fields": {
      "Title": [
        {
          "type": "string"
        },
        {
          "type": "autocomplete"
        }
      ],
      "Authors": [
        {
          "type": "string"
        },
        {
          "type": "autocomplete"
        }
      ],
      "Publisher": [
        {
          "type": "string"
        },
        {
          "type": "autocomplete"
        }
      ],
      "Tags": {
        "type": "string"
      },
      "Description": {
        "type": "string"
      },
      "Annotation": {
        "type": "string"
      },
      "UserId": {
        "type": "objectId"
      },
      "Status": {
        "type": "string"
      },
      "Embedding": {
        "type": "knnVector",
        "dimensions": 3072,
        "similarity": "cosine"
      }
    }
  }
}
```

### How to Create:
1. Go to **MongoDB Atlas** → Your Cluster → **Search**
2. Click **"Create Search Index"** (NOT "Create Vector Search Index")
3. Select **"JSON Editor"**
4. Index Name: `default`
5. Collection: `Books`
6. Paste the JSON configuration above
7. Click **"Create Search Index"**

---

## How It Works

The search uses a **compound query** that combines text and vector searches in a single operation. MongoDB Atlas Search automatically balances the scores.

### Search Query: "Shakespeare"

**Text Search Component:**
- Matches "Shakespeare" in Title, Authors fields
- Uses autocomplete for partial matches
- Fuzzy matching for typos
- Boost values prioritize Title (8.0) and Authors (8.0) over Description (1.0)

**Vector Search Component:**
- Generates embedding for query "Shakespeare"
- Finds books with semantically similar embeddings
- Boost value (10.0) balances vector scores with text scores

**Combined Result:**
- "Romeo and Juliet by William Shakespeare" → **HIGH text score + HIGH vector score** ✅
- "Hamlet" → **HIGH text score + HIGH vector score** ✅
- "Renaissance Drama" → **LOW text score + MEDIUM vector score**
- "Heart of Darkness" → **MEDIUM text score (contains "dark") + LOW vector score**

Books matching both text and semantic meaning rank highest.

---

## How Vector Search Prevents False Positives

### Example: Query "dark novels"

**Text-only would match:**
- ❌ "Heart of Darkness" (contains "dark")
- ❌ "The Novel" (contains "novel")
- ✅ "Dark Fantasy series" (actually relevant)

**Hybrid text + vector matches:**
- ✅ "Dark Fantasy series" → HIGH text + HIGH vector (genre match)
- ✅ "Gothic horror novel" → LOW text + HIGH vector (semantically similar)
- ⚠️ "Heart of Darkness" → MEDIUM text + LOW vector (not about dark fiction genre)

The **vector embedding understands context**, so "Heart of Darkness" (about colonialism) is not semantically similar to "dark novels" (dark fantasy genre), even though both contain the word "dark".

---

## Tuning Search Boosts

In `BookDao.cs:239-250`, you can adjust text field boosts:

```csharp
searchBuilder.Text(f => f.Title, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)),     // Exact title matches
searchBuilder.Text(f => f.Authors, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)),   // Author name matches
searchBuilder.Text(f => f.Publisher, queryParameters.SearchTerm, score: scoreBuilder.Boost(8.0)), // Publisher matches
searchBuilder.Text(f => f.Tags, queryParameters.SearchTerm, score: scoreBuilder.Boost(2.0)),      // Tag matches
searchBuilder.Text(f => f.Description, queryParameters.SearchTerm, score: scoreBuilder.Boost(1.0)), // Description matches
```

In `BookDao.cs:263`, adjust vector search boost:

```csharp
searchBuilder.KnnVector(f => f.Embedding, queryParameters.SearchEmbedding, 100, score: scoreBuilder.Boost(10.0))
```

**To favor exact text matches more:**
- Increase text boosts: `8.0 → 15.0`
- Decrease vector boost: `10.0 → 5.0`

**To favor semantic search more:**
- Decrease text boosts: `8.0 → 5.0`
- Increase vector boost: `10.0 → 20.0`

---

## Verification

After creating the index, verify it's active:

```bash
# Check Atlas UI: Search → Indexes
# You should see:
# - "default" - Search Index - Status: Active
```

Index creation takes **5-15 minutes** depending on collection size.

---

## Troubleshooting

### Error: "Index not found"
- Ensure index name is exactly: `default`
- Wait for index to finish building (check status in Atlas UI)

### No vector search results
- Ensure books have embeddings (check `Embedding` field is populated)
- Verify embedding dimensions are 3072 (text-embedding-3-large)
- Vector search only activates when embeddings are available

### Poor search quality
- Tune text search boosts in `BookDao.cs:239-250`
- Tune vector search boost in `BookDao.cs:263`
- Ensure embeddings are being generated correctly (check database)

### "knnVector" type not supported
- Make sure you created an **Atlas Search Index** (not Vector Search Index)
- The `knnVector` type is only available in Atlas Search indexes
- Recreate the index using the "Create Search Index" option
