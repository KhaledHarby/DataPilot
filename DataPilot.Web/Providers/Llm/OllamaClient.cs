using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataPilot.Web.Providers.Llm;

public class OllamaClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    public string Name => "Ollama";

    public OllamaClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> CompleteAsync(string prompt, LlmRequestOptions opt, CancellationToken ct = default)
    {
        var body = new { model = opt.Model, prompt, stream = false, options = new { temperature = opt.Temperature } };
        using var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/generate", body, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }

    public async Task<string> ChatAsync(IEnumerable<LlmMessage> messages, LlmRequestOptions opt, CancellationToken ct = default)
    {
        var msgs = new List<object>();
        foreach (var m in messages)
        {
            msgs.Add(new { role = m.Role, content = m.Content });
        }
        var body = new { model = opt.Model, messages = msgs, stream = false, options = new { temperature = opt.Temperature } };
        using var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/chat", body, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
        return content ?? string.Empty;
    }
}


