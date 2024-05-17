using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextToImage;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.OpenApi;

using FunctionCalling.Plugins.FlightPlugin;
using FunctionCalling.Plugins.WeatherPlugin;
using FunctionCalling.Plugins.PlacesPlugin;
using FunctionCalling.Plugins.JGEMathPlugin;
using FunctionCalling.Plugins.UnitedStatesPlugin;

using static System.Environment;

using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace FunctionCalling;
    internal class FunctionCalling
    {
        static async Task Main(string[] args)

        {
            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(Environment.ModelName, Environment.AOAIEndPoint, Environment.AOAIKey)
                .Build();

            kernel.ImportPluginFromType<UnitedStatesPlugin>();

            //manual function execution
            OpenAIPromptExecutionSettings settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
            };

            Console.WriteLine($"User>") ;
        
            string prompt = "Write a paragraph to share the population of the United States in 2015. \r\nMake sure to specify how many people, among the population, identify themselves as male and female. \r\nDon't share approximations, please share the exact numbers.";

            Console.WriteLine(prompt);

            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.User, prompt);

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

            Console.WriteLine($"=======================================================");
            Console.WriteLine($"Result: Nothing, but the LLM returned the FunctionToolCalls which we need to execute and add to the Prompt") ;

            var functionCalls = ((OpenAIChatMessageContent)result).GetOpenAIFunctionToolCalls();
            foreach (var functionCall in functionCalls)
            {
                KernelFunction pluginFunction;
                KernelArguments arguments;

                kernel.Plugins.TryGetFunctionAndArguments(functionCall, out pluginFunction, out arguments);

                var functionResult = await kernel.InvokeAsync(pluginFunction!, arguments!);
                var jsonResponse = functionResult.GetValue<object>();
                var json = JsonSerializer.Serialize(jsonResponse);

                Console.WriteLine(json);
                chatHistory.AddMessage(AuthorRole.Tool, json);
            }

            result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);
            Console.WriteLine(result.Content);

            Console.WriteLine("Press Enter to continue and repeat with auto function calling:");
            Console.ReadLine();

            // automatic function calling

            OpenAIPromptExecutionSettings settings_updated = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            var streamingResult = kernel.InvokePromptStreamingAsync(prompt, new KernelArguments(settings_updated));
            await foreach (var streamingResponse in streamingResult)
            {
                Console.Write(streamingResponse);
            }

            Console.ReadLine();
    }
}
