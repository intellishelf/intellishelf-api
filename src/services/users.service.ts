import { Injectable } from "@nestjs/common";
import { InjectModel } from "@nestjs/mongoose";
import { User, UserDocument } from "../models/schemas/user.schema";
import { Model } from "mongoose";

@Injectable()
export class UsersService {
  constructor(
    @InjectModel(User.name) private readonly model: Model<UserDocument>
  ) {}

  async findByName(userName: string): Promise<User | null> {
    return await this.model
      .findOne()
      .exec();
  }
  async findById(id: string): Promise<User | null> {
    return await this.model.findById(id).exec();
  }
}
