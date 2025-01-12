import { ApiProperty } from "@nestjs/swagger";

export class ParseTextRequest {
  @ApiProperty({ type: "string", format: "string" })
  text: string;
}
