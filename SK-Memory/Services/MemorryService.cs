using Microsoft.Extensions.Configuration;
using static Environment;

public class MemoryService()
{
    string apiKey = Environment.AOAIKey;
    string deploymentChatName = Environment.ModelName;
    string deploymentEmbeddingName = Environment.embeddingModel;
    string endpoint = Environment.AOAIEndPoint;

    var embeddingConfig = new AzureOpenAIConfig
    {
        APIKey = apiKey,
        Deployment = deploymentEmbeddingName,
        Endpoint = endpoint,
        APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey
    };

    var chatConfig = new AzureOpenAIConfig
    {
        APIKey = apiKey,
        Deployment = deploymentChatName,
        Endpoint = endpoint,
        APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey
    };

    kernelMemory = new KernelMemoryBuilder()
        .WithAzureOpenAITextGeneration(chatConfig)
        .WithAzureOpenAITextEmbeddingGeneration(embeddingConfig)
        .WithSimpleVectorDb()
        .Build<MemoryServerless>();

    public async Task<bool> StoreFile(string path, string filename)
    {
        try
        {
            await kernelMemory.ImportDocumentAsync(path, filename);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<string> AskQuestion(string question)
    {
        var answer = await kernelMemory.AskAsync(question);

        return answer.Result;
    }
}