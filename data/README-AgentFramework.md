# Microsoft Agent Framework

## What is Microsoft Agent Framework?

Microsoft Agent Framework is a comprehensive SDK designed for building intelligent, conversational AI agents capable of reasoning, planning, and executing complex multi-step tasks. It provides a unified abstraction layer that allows developers to create autonomous agents that can interact with users, make decisions, and take actions based on their understanding of context and goals.

The framework is built on top of the Microsoft.Extensions.AI abstractions, which means it integrates seamlessly with any chat client implementation, including OpenAI, Azure OpenAI, Ollama, and other providers that implement the `IChatClient` interface.

## Core Concepts

### Agents

An agent is an autonomous entity that can:
- Understand natural language instructions
- Maintain conversation context across multiple interactions
- Reason about how to accomplish tasks
- Execute actions using available tools
- Learn from feedback and adjust behavior

### Tools (Functions)

Tools are external capabilities that agents can invoke to interact with the world. These can include:
- Database queries and updates
- API calls to external services
- File system operations
- Code execution
- Web searches

### Memory and Context

Agents maintain memory across conversations, enabling them to:
- Remember previous interactions with users
- Store and retrieve relevant information
- Build long-term understanding of user preferences
- Maintain state across multiple sessions

## Key Features

- **Autonomous Decision Making**: Agents can independently decide which actions to take based on user intent and available tools
- **LLM Integration**: First-class support for OpenAI, Azure OpenAI, Ollama, Anthropic, and custom providers
- **Tool Calling**: Native support for function calling with automatic parameter extraction and validation
- **Conversation Memory**: Built-in mechanisms for persisting and retrieving conversation history
- **Streaming Responses**: Real-time streaming of agent responses for better user experience
- **Structured Output**: Support for JSON schema-based structured responses
- **Multi-Agent Orchestration**: Ability to coordinate multiple agents working together

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    AI Agent                              │
├─────────────────────────────────────────────────────────┤
│  Instructions  │  Tools Registry  │  Memory Store       │
├─────────────────────────────────────────────────────────┤
│                 IChatClient Abstraction                  │
├─────────────────────────────────────────────────────────┤
│  OpenAI  │  Azure OpenAI  │  Ollama  │  Custom Provider │
└─────────────────────────────────────────────────────────┘
```

## Basic Example

```csharp
using Microsoft.Extensions.AI;
using OllamaSharp;

// Create the underlying chat client
IChatClient chatClient = new OllamaApiClient(
    new Uri("http://localhost:11434"), 
    "llama3.2"
);

// Create an agent with instructions
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant that provides concise, accurate answers.",
    name: "HelpfulAssistant"
);

// Run the agent with a user message
var result = await agent.RunAsync(message: "What is the capital of France?");
Console.WriteLine(result.Content);
```

## Agent with Tools Example

```csharp
using Microsoft.Extensions.AI;

// Define a tool function
[Description("Gets the current weather for a location")]
static async Task<string> GetWeather(string location)
{
    // Call weather API
    return $"The weather in {location} is sunny, 22°C";
}

// Create chat client with tools
IChatClient chatClient = new OllamaApiClient(ollamaUri, "llama3.2")
    .AsBuilder()
    .UseFunctions([AIFunctionFactory.Create(GetWeather)])
    .Build();

// Create agent
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You help users with weather information.",
    name: "WeatherAgent"
);

// The agent will automatically call GetWeather when needed
var result = await agent.RunAsync("What's the weather like in Paris?");
```

## Multi-Turn Conversation Example

```csharp
// Create agent with memory
var conversationHistory = new List<ChatMessage>();
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a travel planning assistant.",
    name: "TravelAgent"
);

// First turn
var result1 = await agent.RunAsync(
    message: "I want to plan a trip to Japan",
    history: conversationHistory
);
conversationHistory.AddRange(result1.Messages);

// Second turn - agent remembers context
var result2 = await agent.RunAsync(
    message: "What's the best time to visit?",
    history: conversationHistory
);
```

## Configuration Options

| Option | Type | Description |
|--------|------|-------------|
| `Instructions` | string | System prompt defining agent behavior |
| `Name` | string | Agent identifier for logging and tracing |
| `MaxIterations` | int | Maximum tool-calling iterations (default: 10) |
| `Temperature` | float | Creativity level (0.0-1.0) |
| `MaxTokens` | int | Maximum response length |

## Best Practices

1. **Clear Instructions**: Provide specific, detailed instructions about the agent's role and constraints
2. **Tool Documentation**: Include comprehensive descriptions for all tools to help the agent understand when to use them
3. **Error Handling**: Implement robust error handling for tool failures
4. **Rate Limiting**: Consider rate limiting for production deployments
5. **Logging**: Enable detailed logging for debugging agent behavior

## Integration with Semantic Kernel

Agent Framework integrates seamlessly with Semantic Kernel plugins:

```csharp
var kernel = Kernel.CreateBuilder()
    .AddOllamaChatCompletion("llama3.2", new Uri("http://localhost:11434"))
    .Build();

// Import Semantic Kernel plugins as tools
kernel.ImportPluginFromType<TimePlugin>("time");
kernel.ImportPluginFromType<MathPlugin>("math");

// Use kernel's chat client with agent
var chatClient = kernel.GetRequiredService<IChatClient>();
var agent = chatClient.AsAIAgent(instructions: "...", name: "SKAgent");
```

## Resources

- [Official Documentation](https://learn.microsoft.com/dotnet/ai/)
- [GitHub Repository](https://github.com/microsoft/dotnet-ai)
- [NuGet Package](https://www.nuget.org/packages/Microsoft.Agents.AI)
- [Samples Repository](https://github.com/microsoft/dotnet-ai-samples)

## NuGet Packages

| Package | Description |
|---------|-------------|
| `Microsoft.Agents.AI` | Core agent framework |
| `Microsoft.Extensions.AI` | AI abstractions |
| `Microsoft.Extensions.AI.Ollama` | Ollama integration |
| `Microsoft.Extensions.AI.OpenAI` | OpenAI integration |
