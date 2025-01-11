import { Injectable, UnauthorizedException } from "@nestjs/common";
import { JwtService } from "@nestjs/jwt";
import { UsersService } from "./users.service";

@Injectable()
export class AuthService {
  constructor(
    private readonly usersService: UsersService,
    private readonly jwtService: JwtService
  ) { }

  async signIn(username: string, password: string): Promise<string> {
    const user = await this.usersService.findByName(username);

    if (user?.password === password)
      return await this.jwtService.signAsync({
        userName: user.userName,
        userId: user.id.toString()
      }, {
        expiresIn: '30d'
      });

    throw new UnauthorizedException();
  }
}
