import { Router } from "express";
export const booksRouter = Router();

/**
 * @swagger
 * /books:
 *  get:
 *      description: Returns books
 *      responses:
 *          200:
 *              description: books
 */
booksRouter.get("/", function (req, res) {
  res.json([
    {
      title: "The Great Gatsby",
    },
  ]);
});
