import { Injectable } from "@nestjs/common";
import { ParsedBookResponse } from "../models/dtos/books/parsed-book-response.dto";
import { ConfigService } from "@nestjs/config";
import createClient, {
  ImageAnalysisClient,
  ImageAnalysisResultOutput,
} from "@azure-rest/ai-vision-image-analysis";
import { AzureKeyCredential } from "@azure/core-auth";
import OpenAI from "openai";

@Injectable()
export class AiService {
  // private prompt =
  //   "You are a book page parser. You are given a verso book page which may include all information about book. I want add this to my personal library. Extract content language, title, authors, publisher, publicationDate, pages, isbn, description. Also, include annotationStart (which keeps first 3-5 words of annotation) and annotationEnd (3-5 words of annotation ending). Return as JSON with double quotes. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. Example: {'language': 'ua', 'title': 'Castle', authors: ['Franz Kafka']}";

  private prompt =
    "You are a book page parser. You are given a verso book page which may include all information about book. I want add this to my personal library. Extract content language, title, authors, publisher, publicationDate, pages, isbn, description. Return as JSON with double quotes. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. Example: {'language': 'ua', 'title': 'Castle', authors: ['Franz Kafka']}";

  private imageAnalysisClient: ImageAnalysisClient;
  private openAiClient: OpenAI;

  constructor(private configService: ConfigService) {
    this.imageAnalysisClient = createClient(
      this.configService.get<string>("AZURE_VISION_ENDPOINT")!,
      new AzureKeyCredential(
        this.configService.get<string>("AZURE_VISION_KEY")!
      )
    );

    this.openAiClient = new OpenAI({
      apiKey: this.configService.get<string>("OPENAI_API_KEY")!,
    });
  }

  async parseBook(image: Buffer): Promise<ParsedBookResponse> {
    const result = await this.imageAnalysisClient
      .path("/imageanalysis:analyze")
      .post({
        body: image,
        queryParameters: {
          features: ["Read"],
        },
        contentType: "application/octet-stream",
      });

    const parsingResult = result.body as ImageAnalysisResultOutput;

    const lines = Array<string>();

    if (parsingResult.readResult) {
      parsingResult.readResult.blocks.forEach((block) =>
        block.lines.forEach((l) => lines.push(l.text))
      );
    }

    const chatCompletion = await this.openAiClient.chat.completions.create({
      messages: [
        { role: "user", content: lines.join("\n") },
        { role: "system", content: this.prompt },
      ],
      model: "gpt-3.5-turbo",
      temperature: 0,
    });

    return JSON.parse(chatCompletion.choices[0]!.message.content!);
  }
}
