using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace SK
{
    public static class Environment
    {
 
        public static string ModelName = "deployment name in Azure Open AI";
        public static string imgModelName = "Image deployment name in Azure Open AI";
        public static string AOAIEndPoint = "https://xxxxxx.openai.azure.com/";
        public static string AOAIKey = "your Azure OpenAI Key";
        public static string embeddingModel = "Embedding deployment name in Azure Open AI";
        public static string ApiWeatherKey = "Your Weather API Key";
        public static string ApiPlacesKey = "Your Bing Maps Deployment on Azure Key";
        public static string ApiFlightKey = "Your Flight API Key";
    }
}
