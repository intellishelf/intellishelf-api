import { Request, Response, NextFunction } from "express";

export const authMiddleware = (
  req: Request,
  res: Response,
  next: NextFunction
) => {
  const authHeader = req.headers["api-key"];

  if (authHeader !== process.env.API_KEY) {
    return res.status(401).json({ error: "API Key is missing" });
  }

  next();
};
