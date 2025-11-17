import { useState, useRef, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Send, Bot, User, StopCircle } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useStreamChat } from "@/hooks/chat/useStreamChat";
import type { ChatMessage } from "@/types/chat";
import ReactMarkdown from "react-markdown";

interface Message extends ChatMessage {
  id: string;
}

const Chat = () => {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: "1",
      role: "assistant",
      content: "Hi! I'm your AI book assistant. Ask me anything about your library, get book recommendations, or discuss literature!",
    },
  ]);
  const [input, setInput] = useState("");
  const [error, setError] = useState<string | null>(null);
  const scrollAreaRef = useRef<HTMLDivElement>(null);
  const { sendMessage, isStreaming, cancelStream } = useStreamChat();

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    if (scrollAreaRef.current) {
      const scrollContainer = scrollAreaRef.current.querySelector('[data-radix-scroll-area-viewport]');
      if (scrollContainer) {
        scrollContainer.scrollTop = scrollContainer.scrollHeight;
      }
    }
  }, [messages]);

  const handleSend = async () => {
    if (!input.trim() || isStreaming) return;

    setError(null);
    const userMessage: Message = {
      id: Date.now().toString(),
      role: "user",
      content: input,
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput("");

    // Create placeholder for assistant message
    const assistantMessageId = (Date.now() + 1).toString();
    const assistantMessage: Message = {
      id: assistantMessageId,
      role: "assistant",
      content: "",
    };
    setMessages((prev) => [...prev, assistantMessage]);

    // Prepare chat history (exclude the placeholder we just added)
    const history: ChatMessage[] = messages.map(({ role, content }) => ({
      role,
      content,
    }));

    // Stream the response
    let accumulatedContent = "";
    await sendMessage(input, history, {
      onChunk: (chunk) => {
        accumulatedContent += chunk;
        setMessages((prev) =>
          prev.map((msg) =>
            msg.id === assistantMessageId
              ? { ...msg, content: accumulatedContent }
              : msg
          )
        );
      },
      onComplete: () => {
        console.log("Stream completed");
      },
      onError: (errorMsg) => {
        setError(errorMsg);
        // Remove the empty assistant message on error
        setMessages((prev) => prev.filter((msg) => msg.id !== assistantMessageId));
      },
    });
  };

  return (
    <div className="h-full flex flex-col">
      <header className="border-b border-border p-6">
        <h1 className="text-3xl font-bold text-foreground mb-1">AI Chat</h1>
        <p className="text-muted-foreground">
          Chat with AI about your books and get recommendations
        </p>
      </header>

      <ScrollArea className="flex-1 p-6" ref={scrollAreaRef}>
        <div className="max-w-3xl mx-auto space-y-4">
          {messages.map((message) => (
            <div
              key={message.id}
              className={`flex gap-3 ${
                message.role === "user" ? "justify-end" : "justify-start"
              }`}
            >
              {message.role === "assistant" && (
                <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center flex-shrink-0">
                  <Bot className="w-5 h-5 text-primary-foreground" />
                </div>
              )}
              <Card
                className={`p-4 max-w-[80%] ${
                  message.role === "user"
                    ? "bg-primary text-primary-foreground"
                    : "bg-card"
                }`}
              >
                {message.role === "assistant" ? (
                  <ReactMarkdown
                    className="text-sm prose prose-sm dark:prose-invert max-w-none prose-p:my-2 prose-ul:my-2 prose-ol:my-2 prose-li:my-0"
                  >
                    {message.content || "..."}
                  </ReactMarkdown>
                ) : (
                  <p className="text-sm whitespace-pre-wrap">{message.content}</p>
                )}
              </Card>
              {message.role === "user" && (
                <div className="w-8 h-8 rounded-full bg-secondary flex items-center justify-center flex-shrink-0">
                  <User className="w-5 h-5 text-foreground" />
                </div>
              )}
            </div>
          ))}
          {error && (
            <Card className="p-4 bg-destructive/10 border-destructive">
              <p className="text-sm text-destructive">Error: {error}</p>
            </Card>
          )}
        </div>
      </ScrollArea>

      <div className="border-t border-border p-6">
        <div className="max-w-3xl mx-auto flex gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && !isStreaming && handleSend()}
            placeholder="Ask about your books..."
            className="flex-1 bg-secondary border-border"
            disabled={isStreaming}
          />
          {isStreaming ? (
            <Button onClick={cancelStream} size="icon" variant="destructive">
              <StopCircle className="w-4 h-4" />
            </Button>
          ) : (
            <Button onClick={handleSend} size="icon" disabled={!input.trim()}>
              <Send className="w-4 h-4" />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
};

export default Chat;
