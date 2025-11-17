export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
}

// Backend sends PascalCase properties
export interface ChatStreamChunk {
  Content: string;
  Done: boolean;
  Error?: string;
}
