# MongoDB Atlas Index Configuration

This document describes the required MongoDB Atlas indexes for the IntelliShelf API search functionality.

## Required Indexes

The application uses **hybrid search** combining:
- **Text search** (exact matches, autocomplete, fuzzy matching)
- **Vector search** (semantic similarity via embeddings)

This requires **TWO separate indexes**: one Atlas Search index for text, and one Atlas Vector Search index for embeddings.

**Why two indexes?** MongoDB deprecated `knnVector` in compound queries. The recommended approach now uses dedicated `$vectorSearch` stage which requires a separate Vector Search index.

---

## 1. Atlas Search Index (for text search)

**Index Name:** `default`
**Index Type:** Search Index
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

## 2. Atlas Vector Search Index (for semantic search)

**Index Name:** `vector_index`
**Index Type:** Vector Search Index
**Collection:** `Books`

### Configuration JSON:

```json
{
  "fields": [
    {
      "type": "vector",
      "path": "Embedding",
      "numDimensions": 3072,
      "similarity": "cosine"
    },
    {
      "type": "filter",
      "path": "UserId"
    },
    {
      "type": "filter",
      "path": "Status"
    }
  ]
}
```

### How to Create:
1. Go to **MongoDB Atlas** → Your Cluster → **Search**
2. Click **"Create Search Index"** dropdown → Select **"Atlas Vector Search"**
3. Select **"JSON Editor"**
4. Index Name: `vector_index`
5. Collection: `Books`
6. Paste the JSON configuration above
7. Click **"Create Vector Search Index"**

---

## How It Works

The search uses `$vectorSearch` + `$unionWith` to combine results from both indexes with weighted scoring.

### Search Query: "Shakespeare"

**1. Vector Search (`$vectorSearch`):**
- Finds books semantically similar to "Shakespeare"
- Returns top 50 candidates with similarity scores
- Filters by UserId and optional Status

**2. Text Search (`$search`):**
- Matches "Shakespeare" in Title, Authors, Publisher, Tags, Description
- Uses autocomplete for partial matches
- Fuzzy matching for typos
- Returns top 50 candidates with relevance scores

**3. Score Combination:**
- Text scores are weighted **2x** (prioritizes exact matches)
- Vector scores are weighted **1x** (adds semantic similarity)
- Combined score = `(text_score × 2.0) + (vector_score × 1.0)`
- Deduplicates books appearing in both results

**4. Results:**
- "Romeo and Juliet by William Shakespeare" → **HIGH text (×2) + HIGH vector** ✅ **Top result**
- "Hamlet" → **HIGH text (×2) + HIGH vector** ✅
- "Renaissance Drama" → **LOW text (×2) + MEDIUM vector** (appears lower)
- "Heart of Darkness" → **MEDIUM text (×2) + LOW vector** (appears even lower)

Exact matches rank highest due to 2x text weight.

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

## Tuning Search Weights

In `BookDao.cs:286`, you can adjust the weight multipliers:

```csharp
{ "combined_score", new BsonDocument("$add", new BsonArray
    {
        new BsonDocument("$multiply", new BsonArray { "$vs_score", 1.0 }),  // Vector weight
        new BsonDocument("$multiply", new BsonArray { "$ts_score", 2.0 })   // Text weight (2x)
    })
}
```

**To favor exact text matches even more:**
- Increase text multiplier: `2.0 → 3.0`
- Keep vector multiplier: `1.0`

**To balance text and vector equally:**
- Set both to: `1.0`

**To favor semantic search more:**
- Decrease text multiplier: `2.0 → 1.0`
- Increase vector multiplier: `1.0 → 2.0`

You can also adjust text field boosts in `BookDao.cs:436-476`:
```csharp
{ "score", new BsonDocument("boost", new BsonDocument("value", 8.0)) }  // Title/Authors
{ "score", new BsonDocument("boost", new BsonDocument("value", 5.0)) }  // Publisher
{ "score", new BsonDocument("boost", new BsonDocument("value", 2.0)) }  // Tags
```

---

## Verification

After creating both indexes, verify they're active:

```bash
# Check Atlas UI: Search → Indexes
# You should see:
# 1. "default" - Search Index - Status: Active
# 2. "vector_index" - Vector Search Index - Status: Active
```

Index creation takes **5-15 minutes** depending on collection size.

---

## Troubleshooting

### Error: "Index not found"
- Ensure index names match exactly: `default` and `vector_index`
- Wait for indexes to finish building (check status in Atlas UI)

### Error: "$vectorSearch is not supported"
- You created an **Atlas Search** index instead of **Atlas Vector Search**
- Delete the wrong index and recreate using the correct type
- Make sure to select "Atlas Vector Search" option, not "Search Index"

### No vector search results
- Ensure books have embeddings (check `Embedding` field is populated)
- Verify embedding dimensions are 3072 (text-embedding-3-large)
- Vector search only activates when embeddings are available

### Poor search quality
- Tune score weights in `BookDao.cs:286` (text vs vector balance)
- Tune text field boosts in `BookDao.cs:436-476`
- Ensure embeddings are being generated correctly (check database)

### Books appearing in wrong order
- Increase text weight if exact matches should rank higher: `2.0 → 3.0`
- Increase vector weight if semantic matches should rank higher: `1.0 → 2.0`
