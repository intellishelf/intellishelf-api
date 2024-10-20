import { NestFactory } from "@nestjs/core";
import { SwaggerModule, DocumentBuilder } from "@nestjs/swagger";
import { AppModule } from "./app.module";
import { ConfigService } from "@nestjs/config";

async function bootstrap() {
  const app = await NestFactory.create(AppModule);

  app.enableCors({
    origin: ["http://localhost:3000"], //local frontend
  });

  app.setGlobalPrefix("api");

  const config = new DocumentBuilder()
    .setTitle("intellishelf API")
    .addBearerAuth()
    .addSecurityRequirements("bearer")
    .build();

  const document = SwaggerModule.createDocument(app, config);

  SwaggerModule.setup("swagger", app, document);

  const configService = app.get(ConfigService);
  const port = configService.get("WEBSITES_PORT") || 8080;
  await app.listen(port);
}
bootstrap();
