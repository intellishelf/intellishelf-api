// Usage:
//   mongosh "<connection-string>/intellishelf-experiment" scripts/setup-hybrid-search-indexes.js
//
// Optional env:
//   BOOKS_COLLECTION=Books

const collectionName = process.env.BOOKS_COLLECTION || "Books";

const lexicalDefinition = {
  mappings: {
    dynamic: false,
    fields: {
      Title: [{ type: "string" }, { type: "autocomplete" }],
      Authors: [{ type: "string" }, { type: "autocomplete" }],
      Tags: { type: "string" },
      Description: { type: "string" },
      Annotation: { type: "string" },
      Publisher: [{ type: "string" }, { type: "autocomplete" }],
      UserId: { type: "objectId" },
      Status: { type: "number" }
    }
  }
};

const vectorDefinition = {
  fields: [
    { type: "vector", path: "Embedding", numDimensions: 1536, similarity: "cosine" },
    { type: "filter", path: "UserId" },
    { type: "filter", path: "Status" }
  ]
};

function createOrUpdateIndex(name, definition, type = "search") {
  const existing = db.getCollection(collectionName).getSearchIndexes(name);
  if (existing.length > 0) {
    print(`Updating existing index '${name}'...`);
    db.getCollection(collectionName).updateSearchIndex(name, definition);
    return;
  }

  print(`Creating index '${name}' (${type})...`);
  const createCommand = {
    createSearchIndexes: collectionName,
    indexes: [{ name, definition }]
  };
  if (type === "vectorSearch") {
    createCommand.indexes[0].type = "vectorSearch";
  }
  const result = db.runCommand(createCommand);
  if (result.ok !== 1) {
    throw new Error(`Failed to create index '${name}': ${tojson(result)}`);
  }
}

function waitForReady(name, timeoutSeconds = 120) {
  const startedAt = Date.now();
  while (true) {
    const index = db.getCollection(collectionName).getSearchIndexes(name)[0];
    if (index && index.status === "READY") {
      print(`Index '${name}' is READY.`);
      return;
    }

    if ((Date.now() - startedAt) / 1000 > timeoutSeconds) {
      throw new Error(`Timeout waiting for index '${name}' to become READY.`);
    }

    sleep(1000);
  }
}

createOrUpdateIndex("default", lexicalDefinition, "search");
createOrUpdateIndex("vector_index", vectorDefinition, "vectorSearch");

waitForReady("default");
waitForReady("vector_index");

print("Hybrid search indexes are ready.");
