import { useState, useCallback, useRef } from 'react';
import { streamChat } from '@/lib/chat-api';
import type { ChatMessage } from '@/types/chat';

interface UseStreamChatOptions {
  onChunk?: (content: string) => void;
  onComplete?: () => void;
  onError?: (error: string) => void;
}

export function useStreamChat() {
  const [isStreaming, setIsStreaming] = useState(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  const sendMessage = useCallback(
    async (
      message: string,
      history: ChatMessage[],
      options: UseStreamChatOptions = {}
    ) => {
      const { onChunk, onComplete, onError } = options;

      abortControllerRef.current = new AbortController();
      setIsStreaming(true);

      try {
        for await (const chunk of streamChat(
          { message, history },
          abortControllerRef.current.signal
        )) {
          if (chunk.Error) {
            onError?.(chunk.Error);
            break;
          }

          if (chunk.Content) {
            onChunk?.(chunk.Content);
          }

          if (chunk.Done) {
            onComplete?.();
            break;
          }
        }
      } catch (error) {
        if (error instanceof Error) {
          if (error.name === 'AbortError') {
            // Stream was cancelled, not an error
            console.log('Stream cancelled by user');
          } else {
            onError?.(error.message);
          }
        } else {
          onError?.('An unknown error occurred');
        }
      } finally {
        setIsStreaming(false);
        abortControllerRef.current = null;
      }
    },
    []
  );

  const cancelStream = useCallback(() => {
    abortControllerRef.current?.abort();
  }, []);

  return {
    sendMessage,
    isStreaming,
    cancelStream,
  };
}
