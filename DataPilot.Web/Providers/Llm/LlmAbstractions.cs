using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataPilot.Web.Providers.Llm;

public record LlmMessage(string Role, string Content);
public record LlmRequestOptions(string Model, double Temperature = 0.1, int MaxTokens = 1024);

public enum LlmProvider { OpenAI, Claude, Gemini, Ollama, Azure }

public interface ILLMClient
{
    Task<string> CompleteAsync(string prompt, LlmRequestOptions opt, CancellationToken ct = default);
    Task<string> ChatAsync(IEnumerable<LlmMessage> messages, LlmRequestOptions opt, CancellationToken ct = default);
    string Name { get; }
}
