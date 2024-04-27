import { ApiProperty } from '@nestjs/swagger';

export class LoginRequest {
  @ApiProperty()
  userName!: string;

  @ApiProperty()
  password!: string;
}