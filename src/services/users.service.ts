import { Injectable, NotFoundException } from "@nestjs/common";
import { InjectModel } from "@nestjs/mongoose";
import { User, UserDocument } from "../models/schemas/user.schema";
import { Model } from "mongoose";

@Injectable()
export class UsersService {
  constructor(
    @InjectModel(User.name) private readonly model: Model<UserDocument>
  ) {}

  async findByName(userName: string): Promise<UserDocument | null> {
    return await this.model.findOne({ userName });
  }
  async findById(id: string): Promise<UserDocument> {
    const user = await this.model.findById(id);

    if (user) return user;

    throw new NotFoundException();
  }
}
