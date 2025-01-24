﻿using System.ClientModel;
using Microsoft.Extensions.AI;

using Moq;

using Xunit;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using Azure.AI.OpenAI;
using FluentAssertions.Execution;
using StructuredPrediction;

namespace Planner.Tests;

public class StructuredChatClientTests
{

    private readonly IConfiguration _configuration;

    public StructuredChatClientTests()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddUserSecrets<StructuredChatClientTests>();

        _configuration = builder.Build();
    }

    [Fact]
    public void StructuredChatClient_throws_when_FunctionInvocation_client_is_used()
    {
        Mock<IChatClient> clientMock = new();

        IChatClient chatClient = new FunctionInvokingChatClient(clientMock.Object);
        Func<StructuredChatClient> clientBuild = () => new StructuredChatClient(chatClient, [typeof(Plan)]);
        clientBuild.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void StructuredChatClient_generates_tools_for_each_type()
    {
        Mock<IChatClient> clientMock = new();
        StructuredChatClient client = new(clientMock.Object, [typeof(Plan)]);

        client.GetSupportedTypes().Should().BeEquivalentTo([typeof(Plan)]);
    }

    [Fact]
    public void AIParserTool_generates_a_function_metadata()
    {
        AIParserFunction tool = new(typeof(Plan));
        tool.Metadata.Parameters.Should().HaveCount(1);
        tool.Metadata.ReturnParameter.Should().NotBeNull();
    }

    [Fact]
    public async Task AIParserTool_parses_a_conversation()
    {

        string endpoint = _configuration["AzureOpenAI:Endpoint"] ?? string.Empty;
        string key = _configuration["AzureOpenAI:Key"] ?? string.Empty;
        IChatClient chatClient = new AzureOpenAIClient(
                new Uri(endpoint!),
                new ApiKeyCredential(key!))

            .AsChatClient("gpt-4o-mini");
        StructuredChatClient client = new(chatClient, [typeof(Plan)]);

        StructuredPredictionResult result = await client.PredictAsync([new ChatMessage(ChatRole.User, "create a plan to go to the moon")]);

        using AssertionScope _ = new();

        result.PredictionType.Should().Be(typeof(Plan));

        Plan? plan = result.Value as Plan;

        plan.Should().NotBeNull();
        plan!.Steps.Should().HaveCountGreaterThan(0);

    }

    [Fact]
    public async Task AIParserTool_chooses_one_type_to_parse_a_conversation()
    {
        string endpoint = _configuration["AzureOpenAI:Endpoint"] ?? string.Empty;
        string key = _configuration["AzureOpenAI:Key"] ?? string.Empty;
        IChatClient chatClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(key))

            .AsChatClient("gpt-4o-mini");
        StructuredChatClient client = new(chatClient, [typeof(Plan), typeof(PlanResult)]);

        StructuredPredictionResult result = await client.PredictAsync([
            new ChatMessage(ChatRole.System, "Create a plan if the user asks for help on how to achieve a goal, if is clear what to do then just present a result"),
            new ChatMessage(ChatRole.User, "We got on the moon.")]);

        using AssertionScope _ = new();

        result.PredictionType.Should().Be(typeof(PlanResult));

        PlanResult? planResult = result.Value as PlanResult;

        planResult.Should().NotBeNull();
    }
}
