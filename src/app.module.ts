import { Module } from "@nestjs/common";
import { JwtModule } from "@nestjs/jwt";
import { ConfigModule } from "@nestjs/config";
import { UsersService } from "./services/users.service";
import { AuthController } from "./controllers/auth.controller";
import { AuthService } from "./services/auth.service";
import { MongooseModule } from "@nestjs/mongoose";
import { UserSchema, User } from "./models/schemas/user.schema";

@Module({
  controllers: [AuthController],
  providers: [AuthService, UsersService],
  imports: [
    ConfigModule.forRoot(),
    JwtModule.register({ secret: process.env.PRIVATE_KEY }),
    MongooseModule.forRoot(process.env.DB_CONNECTION_STRING!),
    MongooseModule.forFeature(
      [
        {
          name: User.name,
          schema: UserSchema,
        },
      ]
    ),
  ],
})
export class AppModule {}
