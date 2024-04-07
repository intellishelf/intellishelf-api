import express from "express";
require("dotenv").config();

const app = express();

app.use(express.json());

app.get("/", (req, res) => {
  res.json({ message: "Hi!" });
});

app.listen(3000, () => {
  console.log("Server started on port 3000");
});
