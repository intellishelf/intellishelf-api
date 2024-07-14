import { ApiProperty, ApiPropertyOptional } from "@nestjs/swagger";

export class AddBookRequest {
  @ApiProperty()
  title: string;

  @ApiPropertyOptional()
  authors: string;

  @ApiPropertyOptional()
  publicationDate: Date;

  @ApiPropertyOptional()
  isbn: string;

  @ApiPropertyOptional()
  description: string;

  @ApiPropertyOptional()
  publisher: string;

  @ApiPropertyOptional()
  pages: number;
}
