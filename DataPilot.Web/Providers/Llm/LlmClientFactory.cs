using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace DataPilot.Web.Providers.Llm;

public class LlmClientFactory
{
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmClientFactory(IConfiguration cfg, IHttpClientFactory httpClientFactory)
    {
        _cfg = cfg;
        _httpClientFactory = httpClientFactory;
    }

    public ILLMClient Create(string providerName)
    {
        if (Enum.TryParse<LlmProvider>(providerName, true, out var provider))
        {
            return Create(provider);
        }
        throw new NotSupportedException($"LLM provider '{providerName}' not supported in this minimal setup.");
    }

    public ILLMClient Create(LlmProvider provider)
    {
        var http = _httpClientFactory.CreateClient("llm");
        
        switch (provider)
        {
            case LlmProvider.Ollama:
                var baseUrl = _cfg["LLM:Providers:Ollama:BaseUrl"] ?? "http://localhost:11434";
                return new OllamaClient(http, baseUrl);
                
            case LlmProvider.OpenAI:
                var key = _cfg["LLM:Providers:OpenAI:ApiKey"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("OpenAI ApiKey is not configured. Set LLM:Providers:OpenAI:ApiKey via user secrets or environment.");
                }
                return new OpenAiClient(http, key);
                
            case LlmProvider.Azure:
                var endpoint = _cfg["LLM:Providers:Azure:Endpoint"] ?? string.Empty;
                var azureKey = _cfg["LLM:Providers:Azure:ApiKey"] ?? string.Empty;
                var deployment = _cfg["LLM:Providers:Azure:Deployment"] ?? _cfg["LLM:Model"] ?? string.Empty;
                var apiVersion = _cfg["LLM:Providers:Azure:ApiVersion"] ?? "2024-02-15-preview";
                return new AzureOpenAIClient(http, endpoint, azureKey, deployment, apiVersion);
                
            default:
                throw new NotSupportedException($"LLM provider '{provider}' not supported in this minimal setup.");
        }
    }
}
