import { Prop, Schema, SchemaFactory } from "@nestjs/mongoose";
import { Document, SchemaTypes, Types } from "mongoose";

export type BookDocument = Book & Document;

@Schema()
export class Book {
  @Prop({ type: SchemaTypes.ObjectId })
  _id: Types.ObjectId;

  @Prop({ required: true })
  title: string;

  @Prop({ required: true, type: SchemaTypes.ObjectId })
  userId: Types.ObjectId;

  authors: string;
  publicationDate: Date;
  isbn: string;
  description: string;
  publisher: string;
  pages: number;
}

export const BookSchema = SchemaFactory.createForClass(Book);
