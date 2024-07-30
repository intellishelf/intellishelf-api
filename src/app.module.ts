import { Module } from "@nestjs/common";
import { JwtModule } from "@nestjs/jwt";
import { ConfigModule } from "@nestjs/config";
import { UsersService } from "./services/users.service";
import { AuthController } from "./controllers/auth.controller";
import { AuthService } from "./services/auth.service";
import { UserSchema, User } from "./models/schemas/user.schema";
import { BooksController } from "./controllers/books.controller";
import { Book, BookSchema } from "./models/schemas/book.schema";
import { BooksService } from "./services/books.service";
import { MongooseModule } from "@nestjs/mongoose";
import { AiService } from "./services/ai.service";

@Module({
  controllers: [AuthController, BooksController],
  providers: [AuthService, UsersService, BooksService, AiService],
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
    }),
    JwtModule.register({ secret: process.env.PRIVATE_KEY }),
    MongooseModule.forRoot(process.env.DB_CONNECTION_STRING!),
    MongooseModule.forFeature([
      {
        name: User.name,
        schema: UserSchema,
      },
      {
        name: Book.name,
        schema: BookSchema,
      },
    ]),
  ],
})
export class AppModule {}
