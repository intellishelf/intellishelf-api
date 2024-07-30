import { ApiProperty } from "@nestjs/swagger";

export class ParsedBookResponse {
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
