# MongoDB Atlas Index Configuration

This document describes the required MongoDB Atlas indexes for the IntelliShelf API search functionality.

## Required Indexes

The application uses **Reciprocal Rank Fusion (RRF)** for hybrid search, combining:
- **Text search** (exact matches, autocomplete, fuzzy matching)
- **Vector search** (semantic similarity via embeddings)

This requires **TWO separate indexes** in MongoDB Atlas.

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

### Search Query: "Shakespeare"

**1. Text Search** (higher priority for exact matches):
- Finds books with "Shakespeare" in **Title** or **Authors** → **Rank 1-10**
- RRF Score: `1 / (rank + 1 + 1)` = `0.5` for rank 1, `0.33` for rank 2, etc.

**2. Vector Search** (lower priority for semantic matches):
- Finds books semantically similar to "Shakespeare" (e.g., Renaissance drama, Elizabethan literature) → **Rank 1-50**
- RRF Score: `1 / (rank + 60 + 1)` = `0.016` for rank 1, `0.015` for rank 2, etc.

**3. Combined Results**:
- Book "Romeo and Juliet by William Shakespeare":
  - Text rank: #1 (score: 0.5)
  - Vector rank: #5 (score: 0.015)
  - **Final score: 0.515** ✅ **HIGHEST**

- Book "Hamlet by William Shakespeare":
  - Text rank: #2 (score: 0.33)
  - Vector rank: #3 (score: 0.016)
  - **Final score: 0.346**

- Book "Renaissance Drama" (no "Shakespeare" in title/author):
  - Text rank: N/A (score: 0)
  - Vector rank: #10 (score: 0.014)
  - **Final score: 0.014** (appears lower in results)

### Result:
✅ Exact matches appear **first**
✅ Semantically related books appear **after**
✅ Best of both worlds!

---

## Tuning RRF Priorities

In `BookDao.cs:234-235`, you can adjust the priorities:

```csharp
const int textPriority = 1;    // Lower = higher importance (exact matches)
const int vectorPriority = 60; // Higher = lower importance (semantic matches)
```

**To favor exact matches more:**
- Decrease `textPriority` (e.g., 1 → 0)
- Increase `vectorPriority` (e.g., 60 → 100)

**To favor semantic search more:**
- Increase `textPriority` (e.g., 1 → 10)
- Decrease `vectorPriority` (e.g., 60 → 30)

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

### No vector search results
- Ensure books have embeddings (check `Embedding` field is populated)
- Verify embedding dimensions are 3072 (text-embedding-3-large)

### Poor search quality
- Adjust RRF priorities in `BookDao.cs:234-235`
- Tune text search boosts in `BookDao.cs:533-573`
