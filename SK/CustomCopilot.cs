using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextToImage;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.OpenApi;

using SK.CustomCopilot.Plugins.FlightPlugin;
using SK.CustomCopilot.Plugins.WeatherPlugin;
using SK.CustomCopilot.Plugins.PlacesPlugin;
using SK.CustomCopilot.Plugins.JGEMathPlugin;
using SK.CustomCopilot.Plugins.UnitedStatesPlugin;

using static System.Environment;

namespace SK.CustomCopilot
{
    internal class CustomCopilot
    {

        static async Task Main(string[] args)

        {
            // Create a kernel with the Azure OpenAI chat completion service
            var builder = Kernel.CreateBuilder();

            #pragma warning disable SKEXP0050
            #pragma warning disable SKEXP0010
            #pragma warning disable SKEXP0012
            builder.AddAzureOpenAIChatCompletion(Environment.ModelName, Environment.AOAIEndPoint, Environment.AOAIKey);
            builder.AddAzureOpenAITextToImage(Environment.imgModelName, Environment.AOAIEndPoint, Environment.AOAIKey);
            builder.AddOpenAITextEmbeddingGeneration(Environment.embeddingModel, Environment.AOAIEndPoint, Environment.AOAIKey);

            //Logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();               
                logging.SetMinimumLevel(LogLevel.Trace);
            });


            //add some plugins
            builder.Plugins.AddFromType<TimePlugin>();
            builder.Plugins.AddFromObject(new JGEMathPlugin(), nameof(JGEMathPlugin));
            builder.Plugins.AddFromObject(new UnitedStatesPlugin(), nameof(UnitedStatesPlugin));
            builder.Plugins.AddFromObject(new FlightPlugin(Environment.ApiFlightKey), nameof(FlightPlugin));         
            builder.Plugins.AddFromObject(new PlacesPlugin(Environment.ApiPlacesKey), nameof(PlacesPlugin));
            builder.Plugins.AddFromObject(new WeatherPlugin(Environment.ApiWeatherKey), nameof(WeatherPlugin));

            var pluginsDirectory = Path.Combine("C:\\Users\\jangezels\\source\\repos\\SK\\SK", "Plugins", "MailPlugin");
            builder.Plugins.AddFromPromptDirectory(pluginsDirectory, "MailPlugin");

            // Build the kernel
            var kernel = builder.Build();

            // Add the Hotel plugin using the plugin manifest URL, this is done after the kernel build as it is a external plugin
            #pragma warning disable SKEXP0040
            await kernel.ImportPluginFromOpenApiAsync("HotelPlugin", new Uri("http://jgebookingapi.azurewebsites.net/openapi.yaml"));                      

            // Create chat history
            ChatHistory history = [];
            history.AddSystemMessage(@"You're a virtual assistant that helps people find information.");

            // Get chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // get the Text to Image service
            #pragma warning disable SKEXP0001
            var TextToImageService = kernel.GetRequiredService<ITextToImageService>();

            // Start the conversation
            while (true)
            {
                // Get user input
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("User > ");
                history.AddUserMessage(Console.ReadLine()!);

                // Enable auto function calling
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    MaxTokens = 800,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                // Get the response from the AI
                // Breakpoint => Watch Window => Debug.Print kernel.Plugins
                var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: kernel);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nAssistant > ");

                string combinedResponse = string.Empty;
                await foreach (var message in response)
                {
                    //Write the response to the console
                    Console.Write(message);
                    combinedResponse += message;
                }

                Console.WriteLine();

                // Add the message from the agent to the chat history
                history.AddAssistantMessage(combinedResponse);
            }
        }
    }
}