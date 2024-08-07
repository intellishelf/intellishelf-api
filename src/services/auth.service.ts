import { Injectable, UnauthorizedException } from "@nestjs/common";
import { UsersService } from "./users.service";
import { JwtService } from "@nestjs/jwt";

@Injectable()
export class AuthService {
  constructor(
    private readonly usersService: UsersService,
    private readonly jwtService: JwtService
  ) {}

  async signIn(username: string, password: string): Promise<string> {
    const user = await this.usersService.findByName(username);

    if (user?.password === password)
      return this.jwtService.sign({
        userName: user.userName,
        userId: user.id.toString(),
      });

    throw new UnauthorizedException();
  }
}
