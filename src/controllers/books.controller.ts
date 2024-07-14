import {
  Controller,
  Request,
  HttpCode,
  HttpStatus,
  Get,
  UseGuards,
} from "@nestjs/common";
import { ApiTags } from "@nestjs/swagger";
import { AuthGuard } from "../common/auth.guard";
import { AuthorizedRequest } from "../common/authorizedRequest";
import { BookResponse } from "../models/dtos/books/book-response.dto";
import { BooksService } from "../services/books.service";

@ApiTags("books")
@Controller("books")
export class BooksController {
  constructor(
    private readonly booksService: BooksService
  ) {}

  @UseGuards(AuthGuard)
  @HttpCode(HttpStatus.OK)
  @HttpCode(HttpStatus.NOT_FOUND)
  @Get()
  async me(@Request() req: AuthorizedRequest): Promise<BookResponse[]> {
    return await this.booksService.getUserBooks(req.userId);
  }
}
