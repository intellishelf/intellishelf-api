import { ApiProperty } from "@nestjs/swagger";

export class ParseImageRequest {
  @ApiProperty({ type: "string", format: "binary" })
  file: any;
}
