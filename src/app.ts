import express from "express";
import path from "path";
import swaggerJsdoc from "swagger-jsdoc";
import swaggerUi from "swagger-ui-express";
require("dotenv").config();

import { booksRouter } from "./routes/books";

const app = express();

app.use(express.json());

var options = {
  swaggerDefinition: {
    info: {
      title: "My API",
      version: "1.0.0",
      description: "My API for doing cool stuff!",
    },
  },
  apis: [path.join(__dirname, "/routes/*.*")],
};
var swaggerSpecs = swaggerJsdoc(options);

app.use("/books", booksRouter);

app.use("/swagger", swaggerUi.serve, swaggerUi.setup(swaggerSpecs));

app.listen(3000, () => {
  console.log("Server started on port 3000");
});
