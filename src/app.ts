import express from "express";
import path from "path";
import swaggerJsdoc from "swagger-jsdoc";
import swaggerUi from "swagger-ui-express";
require("dotenv").config();

import { booksRouter } from "./routes/books.route";

const app = express();

app.use(express.json());

var options = {
  swaggerDefinition: {
    info: {
      title: "Intellishelf API",
      version: "1.0.0",
      description: "Intellishelf API",
    },
  },
  apis: [path.join(__dirname, "/routes/*.*")],
};

var swaggerSpecs = swaggerJsdoc(options);

app.use("/books", booksRouter);

app.use("/swagger", swaggerUi.serve, swaggerUi.setup(swaggerSpecs));

app.listen(process.env.PORT || 8080, () => {
  console.log("Server started on port " + process.env.PORT);
});
