import { Injectable, NotFoundException } from "@nestjs/common";
import { InjectModel } from "@nestjs/mongoose";
import { Model, Types } from "mongoose";
import { Book, BookDocument } from "../models/schemas/book.schema";
import { AddBookRequest } from "../models/dtos/books/add-book-request.dto";

@Injectable()
export class BooksService {
  constructor(
    @InjectModel(Book.name) private readonly model: Model<BookDocument>
  ) {}

  async getBooks(userId: string): Promise<BookDocument[]> {
    return await this.model.find({ userId: userId });
  }

  async addBook(userId: string, book: AddBookRequest): Promise<string> {
    const { id } = await this.model.create({
      ...book,
      userId: new Types.ObjectId(userId),
      createdDate: new Date(),
      authors: book.authors.split(","),
    });

    return id;
  }

  async deleteBook(userId: string, bookId: string) {
    const book = await this.model.findById(bookId);

    if (book?.userId.toString() === userId) {
      return await this.model.deleteOne({ _id: bookId });
    }

    throw new NotFoundException("Book not found");
  }
}
