import express, { json } from "express";
const app = express();
app.use(json());

app.get("/", (req, res) => {
  res.json({ message: "Hello, world!" });
});

app.listen(3000, () => {
  console.log("Server started on port 3000");
});
