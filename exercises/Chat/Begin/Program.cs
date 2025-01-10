using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ClientModel;

// Set up DI etc
var hostBuilder = Host.CreateApplicationBuilder(args);
hostBuilder.Configuration.AddUserSecrets<Program>();
hostBuilder.Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Register an IChatClient
var azureOpenAiConfig = hostBuilder.Configuration.GetRequiredSection("AzureOpenAI");
var innerChatClient = new AzureOpenAIClient(new Uri(azureOpenAiConfig["Endpoint"]!), new ApiKeyCredential(azureOpenAiConfig["Key"]!))
    .AsChatClient("gpt-4o-mini");
// Or for Ollama:
//var innerChatClient = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1");

hostBuilder.Services.AddChatClient(innerChatClient);

// Run the app
var app = hostBuilder.Build();
var chatClient = app.Services.GetRequiredService<IChatClient>();

var response = await chatClient.CompleteAsync(
    "Explain how real AI compares to sci-fi AI in max 200 words.");

Console.WriteLine(response.Message.Text);
Console.WriteLine($"Tokens used: in={response.Usage?.InputTokenCount}, out={response.Usage?.OutputTokenCount}");

if (response.RawRepresentation is OpenAI.Chat.ChatCompletion openAICompletion)
{
    Console.WriteLine($"Fingerprint: {openAICompletion.SystemFingerprint}");
}

var responseStream = chatClient.CompleteStreamingAsync(
    "Explain how real AI compares to sci-fi AI in max 20 words.");

await foreach (var message in responseStream)
{
    Console.WriteLine(message.Text);
}
