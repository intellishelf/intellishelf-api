import { Router } from "express";
export const booksRouter = Router();

import books from "../mockData/books.json";
import { authMiddleware } from "../middlewares/auth.middleware";

booksRouter.use(authMiddleware);

/**
 * @swagger
 * /books:
 *   parameters:
 *     - in: header
 *       name: Api-Key
 *       required: true
 *       schema:
 *         type: string
 *       description: Bearer token for authentication
 *   get:
 *     description: Returns books
 *     tags: ["books"]
 *     responses:
 *       '200':
 *         description: books
 */
booksRouter.get("/", function (req, res) {
  res.json(books);
});
