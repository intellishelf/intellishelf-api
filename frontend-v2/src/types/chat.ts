export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
}

export enum ChunkType {
  Content = 0,
  ToolCall = 1,
}

// Backend sends PascalCase properties
export interface ChatStreamChunk {
  Content: string;
  Done: boolean;
  Error?: string;
  Type: ChunkType;
  ToolCallDescription?: string;
}
