using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Buffers.Text;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Net.Http;

//using Microsoft.DotNet.Interactive;
//using Microsoft.DotNet.Interactive.AIUtilities;

using SkiaSharp;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;

using static PictureGenerator.Environment;
using static System.Net.Mime.MediaTypeNames;

using Azure.AI.OpenAI;
using System.ComponentModel.DataAnnotations;

namespace PictureGenerator.Program
{
    internal class ImageCreator
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();

            #pragma warning disable SKEXP0010
            builder.AddAzureOpenAIChatCompletion(PictureGenerator.Environment.ModelName, PictureGenerator.Environment.AOAIEndPoint, PictureGenerator.Environment.AOAIKey);
            builder.AddAzureOpenAITextToImage(PictureGenerator.Environment.imgModelName, PictureGenerator.Environment.AOAIEndPoint, PictureGenerator.Environment.AOAIKey);

            // Build the kernel
            var kernel = builder.Build();

            // Get imageToText Service
            #pragma warning disable SKEXP0001
            var dallE = kernel.GetRequiredService<ITextToImageService>();
            
            var prompt = @"Think about an artificial object that represents {{$input}}.";
        
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 256,
                Temperature = 1
            };
            
            // create a semantic function from the prompt
            var genImgFunction = kernel.CreateFunctionFromPrompt(prompt, executionSettings);

            // Get a phrase from the user
            Console.WriteLine("Enter a phrase to generate an image from: ");
            string? phrase = Console.ReadLine();
            if (string.IsNullOrEmpty(phrase))
            {
                Console.WriteLine("No phrase entered.");
                return;
            }

            // Invoke the semantic function to generate an image description
            var imageDescResult = await kernel.InvokeAsync(genImgFunction, new() { ["input"] = phrase });
            var imageDesc = imageDescResult.ToString();

            // Use DALL-E 3 to generate an image. 
            // In this case, OpenAI returns a URL (though you can ask to return a base64 image)
        
            #pragma warning disable SKEXP0001
            var imageUrl = await dallE.GenerateImageAsync(imageDesc.Trim(), 1024, 1024);
            Console.WriteLine(imageUrl);
            var surface = await ShowImage(imageUrl.ToString(), 1024, 1024);
            //surface.Display();  //needs version 2.88.6 not later as it doesn't have the display method


        }
        public static async Task<SKSurface> ShowImage(string url, int width, int height)
        {
            SKImageInfo info = new SKImageInfo(width, height);
            SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var httpClient = new HttpClient();

            using (Stream stream = await httpClient.GetStreamAsync(url))
            using (MemoryStream memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                SKBitmap webBitmap = SKBitmap.Decode(memStream);
                canvas.DrawBitmap(webBitmap, 0, 0, null);
                surface.Draw(canvas, 0, 0, null);
            };
            return surface;
        }
    }
}