import { ApiProperty } from "@nestjs/swagger";

export class UserResponse {
  @ApiProperty()
  public userId: string;

  @ApiProperty()
  public userName: string;
}
