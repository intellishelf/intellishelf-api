import { ApiProperty } from "@nestjs/swagger";

export class DeleteBookRequest {
  @ApiProperty()
  bookId: string;
}
