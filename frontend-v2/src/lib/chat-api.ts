import type { ChatRequest, ChatStreamChunk } from '@/types/chat';
import { API_URL } from './api';

export async function* streamChat(
  request: ChatRequest,
  signal?: AbortSignal
): AsyncGenerator<ChatStreamChunk> {
  const response = await fetch(`${API_URL}/chat-stream`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();

      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const jsonStr = line.slice(6);
          if (jsonStr.trim()) {
            const chunk = JSON.parse(jsonStr) as ChatStreamChunk;
            yield chunk;
            if (chunk.done) return;
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}
