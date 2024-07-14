import {
  Injectable,
  CanActivate,
  ExecutionContext,
  UnauthorizedException,
} from "@nestjs/common";
import { JwtService } from "@nestjs/jwt";
import { AuthorizedRequest } from "./authorizedRequest";

@Injectable()
export class AuthGuard implements CanActivate {
  constructor(private readonly jwtService: JwtService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest() as AuthorizedRequest;

    const token = request.headers.authorization?.split(" ")[1];

    if (!token) throw new UnauthorizedException();

    try {
      const { userId } = await this.jwtService.verifyAsync(token);

      request.userId = userId;

      return true;
    } catch {
      throw new UnauthorizedException();
    }
  }
}
