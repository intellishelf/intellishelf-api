import {
  Body,
  Controller,
  Post,
  Request,
  HttpCode,
  HttpStatus,
  Get,
  UseGuards,
  HttpException,
} from "@nestjs/common";
import { ApiTags } from "@nestjs/swagger";
import { LoginRequest } from "../models/dtos/auth/login-request.dto";
import { AuthService } from "../services/auth.service";
import { AuthGuard } from "../common/auth.guard";
import { UsersService } from "../services/users.service";
import { UserResponse } from "../models/dtos/auth/user-response.dto";
import { AuthorizedRequest } from "../common/authorizedRequest";

@ApiTags("auth")
@Controller("auth")
export class AuthController {
  constructor(
    private readonly authService: AuthService,
    private readonly userService: UsersService
  ) {}

  @HttpCode(HttpStatus.OK)
  @Post("login")
  async login(@Body() loginReques: LoginRequest): Promise<string> {
    return await this.authService.signIn(loginReques.userName, loginReques.password);
  }

  @UseGuards(AuthGuard)
  @HttpCode(HttpStatus.OK)
  @HttpCode(HttpStatus.NOT_FOUND)
  @Get("me")
  async me(@Request() req: AuthorizedRequest): Promise<UserResponse> {
    const user = await this.userService.findById(req.userId);

    if (user === undefined || user === null)
      throw new HttpException("Not found", HttpStatus.NOT_FOUND);

    return {
      userId: user._id.toString(),
      userName: user.userName,
    };
  }
}
