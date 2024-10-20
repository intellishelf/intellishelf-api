import { Injectable, UnauthorizedException } from "@nestjs/common";
import { JwtService } from "@nestjs/jwt";
import { UsersService } from "./users.service";

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
