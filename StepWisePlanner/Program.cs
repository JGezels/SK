using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;

using StepWisePlanner.Plugins.FlightPlugin;
using StepWisePlanner.Plugins.WeatherPlugin;
using StepWisePlanner.Plugins.JGEMathPlugin;
using StepWisePlanner.Plugins.UnitedStatesPlugin;

using static System.Environment;
using System.Text.Json;

namespace StepWisePlanner;
internal class StepWisePlanner
{
    static async Task Main(string[] args)

    {
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(Environment.ModelName, Environment.AOAIEndPoint, Environment.AOAIKey)
            .Build();

        kernel.ImportPluginFromType<UnitedStatesPlugin>();
        kernel.Plugins.AddFromObject(new JGEMathPlugin(), nameof(JGEMathPlugin));

        var pluginsDirectory = Path.Combine("C:\\Users\\jangezels\\source\\repos\\SK\\SK", "Plugins", "MailPlugin");
        kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "MailPlugin");            


        // Add the Hotel plugin using the plugin manifest URL, this is done after the kernel build as it is a external plugin
        #pragma warning disable SKEXP0040
        await kernel.ImportPluginFromOpenApiAsync("HotelPlugin", new Uri("http://jgebookingapi.azurewebsites.net/openapi.yaml"));

        #pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning disable SKEXP0061 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        while (true)
        {

            // Get user input
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("User > ");
            var ask = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Assistant > ");

            // Create the plan
            var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
            var plan = await planner.CreatePlanAsync(kernel, ask);

            // Print the plan to the console
            Console.WriteLine($"Plan: {plan}");

            // Execute the plan
            var result = await plan.InvokeAsync(kernel);

            // Print the result to the console
            Console.WriteLine($"==================");
            Console.WriteLine($"Results: {result}");
            Console.ReadLine();
            Console.Clear();

            Console.WriteLine($"You notice that there is a clear Chain of thought");
            Console.WriteLine($"You can save and reuse these plans. (Saving Tokens!)");
            Console.WriteLine($"You can still intervene if you want");

            Console.ReadLine();
            Console.Clear();
        }
    }
    #pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    #pragma warning restore SKEXP0061 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}

