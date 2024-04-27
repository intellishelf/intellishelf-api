import { Injectable, UnauthorizedException } from "@nestjs/common";
import { UsersService } from "./users.service";
import { JwtService } from "@nestjs/jwt";

@Injectable()
export class AuthService {
  constructor(
    private readonly usersService: UsersService,
    private readonly jwtService: JwtService
  ) {}

  async signIn(
    username: string,
    pass: string
  ): Promise<{ access_token: string }> {
    const user = await this.usersService.findByName(username);

    if (user?.password !== pass) throw new UnauthorizedException();

    return {
      access_token: this.jwtService.sign({
        userName: user.userName,
        userId: user.userId,
      }),
    };
  }
}
