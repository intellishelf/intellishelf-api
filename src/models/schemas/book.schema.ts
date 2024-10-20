import { Prop, Schema, SchemaFactory } from "@nestjs/mongoose";
import { Document, SchemaTypes, Types } from "mongoose";

export type BookDocument = Book & Document;

@Schema()
export class Book {
  @Prop({ required: true, type: SchemaTypes.ObjectId })
  userId: Types.ObjectId;

  @Prop({ required: true })
  createdDate: Date;

  @Prop({ required: true })
  title: string;

  @Prop()
  authors: string[];

  @Prop()
  publicationDate: Date;

  @Prop()
  isbn: string;

  @Prop()
  annotation: string;

  @Prop()
  description: string;

  @Prop()
  publisher: string;

  @Prop()
  pages: number;

  @Prop()
  imageUrl: string;
}

export const BookSchema = SchemaFactory.createForClass(Book);
