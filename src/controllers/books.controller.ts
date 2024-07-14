import {
  Controller,
  Request,
  Get,
  UseGuards,
  Post,
  Delete,
  Param,
  Body,
} from "@nestjs/common";
import { ApiCreatedResponse, ApiOkResponse, ApiTags } from "@nestjs/swagger";
import { AuthGuard } from "../common/auth.guard";
import { AuthorizedRequest } from "../common/authorizedRequest";
import { BookResponse } from "../models/dtos/books/book-response.dto";
import { BooksService } from "../services/books.service";
import { DeleteBookRequest } from "../models/dtos/books/delete-book-request.dto";
import { AddBookRequest } from "../models/dtos/books/add-book-request.dto";
import { BookDocument } from "../models/schemas/book.schema";

@ApiTags("books")
@Controller("books")
export class BooksController {
  constructor(private readonly booksService: BooksService) {}

  @Get()
  @UseGuards(AuthGuard)
  @ApiOkResponse()
  async getBooks(@Request() req: AuthorizedRequest): Promise<BookResponse[]> {
    const books = await this.booksService.getBooks(req.userId);

    return books.map(mapBook);
  }

  @UseGuards(AuthGuard)
  @ApiCreatedResponse({
    type: BookResponse,
  })
  @Post()
  async addBook(
    @Request() req: AuthorizedRequest,
    @Body() params: AddBookRequest
  ): Promise<string> {
    return await this.booksService.addBook(req.userId, params);
  }

  @UseGuards(AuthGuard)
  @Delete(":bookId")
  async deleteBook(
    @Request() req: AuthorizedRequest,
    @Param() params: DeleteBookRequest
  ) {
    await this.booksService.deleteBook(req.userId, params.bookId);
  }
}

const mapBook = (book: BookDocument): BookResponse => {
  return {
    id: book.id,
    title: book.title,
    authors: book.authors,
    publicationDate: book.publicationDate,
    isbn: book.isbn,
    description: book.description,
    publisher: book.publisher,
    pages: book.pages,
  };
};
