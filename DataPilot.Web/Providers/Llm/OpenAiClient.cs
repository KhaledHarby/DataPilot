using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataPilot.Web.Providers.Llm;

public class OpenAiClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    public string Name => "OpenAI";

    public OpenAiClient(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<string> CompleteAsync(string prompt, LlmRequestOptions opt, CancellationToken ct = default)
    {
        var messages = new List<LlmMessage> { new("user", prompt) };
        return await ChatAsync(messages, opt, ct);
    }

    public async Task<string> ChatAsync(IEnumerable<LlmMessage> messages, LlmRequestOptions opt, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var msgList = new List<object>();
        foreach (var m in messages)
        {
            msgList.Add(new { role = m.Role, content = m.Content });
        }
        var body = new
        {
            model = opt.Model,
            messages = msgList,
            temperature = opt.Temperature,
            max_tokens = opt.MaxTokens,
            stream = false
        };
        req.Content = JsonContent.Create(body);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new System.InvalidOperationException($"OpenAI error {(int)resp.StatusCode}: {resp.ReasonPhrase}. Body: {err}");
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? string.Empty;
    }
}


