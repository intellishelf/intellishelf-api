import { ApiProperty } from "@nestjs/swagger";

export class BookResponse {
  title: string;
  authors: string;
  publicationDate: Date;
  isbn: string;
  description: string;
  publisher: string;
  pages: number;
}
