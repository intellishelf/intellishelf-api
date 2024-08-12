import { Context, HttpRequest } from "@azure/functions";
import { AzureHttpAdapter, AzureRequest } from "@nestjs/azure-func-http";
import { createApp } from "../src/main.azure";
import { INestApplication } from "@nestjs/common";

function createPseudoApp(
  createApp: () => Promise<INestApplication>
): () => Promise<INestApplication> {
  return async (): Promise<INestApplication> => {
    const app = await createApp();
    
    const pseudoApp: INestApplication = {
      ...app,
      getHttpAdapter: () => ({
        ...app.getHttpAdapter(),
        getInstance: () => (req: AzureRequest, res: any) => {
          const originalDone = req.context.done;
          req.context.done = (err?: string | Error, result?: any) => {
            if (typeof res.writeHead === 'function') {
              res.writeHead();
            }
            originalDone(err, result);
          };
          app.getHttpAdapter().getInstance()(req, res);
        },
      }),
    };

    return pseudoApp;
  };
}

export default function (context: Context, req: HttpRequest): void {
  AzureHttpAdapter.handle(createPseudoApp(createApp), context, req);
}