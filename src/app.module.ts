import { Module } from "@nestjs/common";
import { JwtModule } from "@nestjs/jwt";
import { ConfigModule } from "@nestjs/config";
import { UsersService } from "./services/users.service";
import { AuthController } from "./controllers/auth.controller";
import { AuthService } from "./services/auth.service";  

@Module({
  controllers: [AuthController],
  providers: [AuthService, UsersService],
  imports: [
    ConfigModule.forRoot(),
    JwtModule.register({ secret: process.env.PRIVATE_KEY }),
  ],
})
export class AppModule {}
