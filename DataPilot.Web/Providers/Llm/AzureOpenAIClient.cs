using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataPilot.Web.Providers.Llm;

public class AzureOpenAIClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _endpoint; // e.g. https://my-resource.openai.azure.com
    private readonly string _apiKey;
    private readonly string _deployment; // model deployment name
    private readonly string _apiVersion;
    public string Name => "AzureOpenAI";

    public AzureOpenAIClient(HttpClient http, string endpoint, string apiKey, string deployment, string apiVersion)
    {
        _http = http;
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _deployment = deployment;
        _apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? "2024-02-15-preview" : apiVersion;
    }

    public async Task<string> CompleteAsync(string prompt, LlmRequestOptions opt, CancellationToken ct = default)
    {
        var messages = new List<LlmMessage> { new("user", prompt) };
        return await ChatAsync(messages, opt, ct);
    }

    public async Task<string> ChatAsync(IEnumerable<LlmMessage> messages, LlmRequestOptions opt, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.Add("api-key", _apiKey);

        var msgList = new List<object>();
        foreach (var m in messages)
        {
            msgList.Add(new { role = m.Role, content = m.Content });
        }
        var body = new
        {
            messages = msgList,
            temperature = opt.Temperature,
            max_tokens = opt.MaxTokens,
            stream = false
        };
        req.Content = JsonContent.Create(body);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? string.Empty;
    }
}


