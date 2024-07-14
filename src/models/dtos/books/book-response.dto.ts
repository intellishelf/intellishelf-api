import { ApiProperty } from "@nestjs/swagger";

export class BookResponse {
  @ApiProperty()
  id: string;

  @ApiProperty()
  title: string;

  @ApiProperty()
  authors: string;

  @ApiProperty()
  publicationDate: Date;

  @ApiProperty()
  isbn: string;

  @ApiProperty()
  description: string;

  @ApiProperty()
  publisher: string;
  
  @ApiProperty()
  pages: number;
}
