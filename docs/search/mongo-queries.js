


const term = "boo";

const userId = ObjectId("66534779e19349a22d2e89ce");

const searchStage = {
  $search: {
    compound: {
      filter: [
        {
          equals: {
            path: "UserId",
            value: userId
          }
        }
      ],
      should: [
        {
          autocomplete: {
            query: term,
            path: "Title",
            score: { boost: { value: 3 } } // prefix/autocomplete matches
          }
        },
        {
          text: {
            query: term,
            path: "Title",
            score: { boost: { value: 8 } } // full match (edit distance 0) ranked highest
          }
        },
        {
          text: {
            query: term,
            path: "Title",
            fuzzy: { maxEdits: 1, prefixLength: 2 },
            score: { boost: { value: 2 } } // remaining fuzzy matches
          }
        }
      ],
      minimumShouldMatch: 1
    }
  }
};

const projectStage = {
  $project: {
    Title: 1,
    UserId: 1,
    score: { $meta: "searchScore" }
  }
};

db.Books.aggregate([searchStage, { $limit: 5 }, projectStage]);