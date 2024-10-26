import {
  Body,
  Controller,
  Post,
  Request,
  Get,
  UseGuards,
  HttpCode,
} from "@nestjs/common";
import { ApiOkResponse, ApiTags } from "@nestjs/swagger";
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

  @Post("login")
  @HttpCode(200)  
  async login(@Body() loginRequest: LoginRequest): Promise<{ token: string }> {
    return {
      token: await this.authService.signIn(
        loginRequest.userName,
        loginRequest.password
      ),
    };
  }

  @Get("me")
  @UseGuards(AuthGuard)
  @ApiOkResponse({ type: UserResponse })
  async me(@Request() req: AuthorizedRequest): Promise<UserResponse> {
    const { id, userName } = await this.userService.findById(req.userId);

    return {
      userId: id,
      userName: userName,
    };
  }
}
