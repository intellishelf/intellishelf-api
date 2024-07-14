import { Injectable } from "@nestjs/common";
import { InjectModel } from "@nestjs/mongoose";
import { Model } from "mongoose";
import { Book, BookDocument } from "../models/schemas/book.schema";

@Injectable()
export class BooksService {
  constructor(
    @InjectModel(Book.name) private readonly model: Model<BookDocument>
  ) {}

  async getUserBooks(userId: string): Promise<Book[]> {
    return await this.model.find({userId: userId}).exec();
  }
}
