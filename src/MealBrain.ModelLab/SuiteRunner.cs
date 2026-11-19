using System.Text.Json;

public sealed record SuiteTurn(string CaseId, int Repetition, int Turn, string ConversationId, IReadOnlyList<Message> Messages);
public sealed record ChatReply(string Content, long ElapsedMs);
public sealed record CaseExecution(string CaseId, int Repetition, string ConversationId,
    string Status, int CompletedTurns, long? FirstResponseMs, string? Error);

public static class SuiteRunner
{
    public static async Task<List<CaseExecution>> RunAsync(EvaluationSuite suite, string prompt,
        Func<SuiteTurn, Task<ChatReply>> send,
        Func<IReadOnlyList<CaseExecution>, Task> checkpoint,
        Action<string>? progress = null)
    {
        var results = new List<CaseExecution>();
        foreach (var item in suite.Cases)
        {
            for (var repetition = 1; repetition <= item.Repetitions; repetition++)
            {
                var conversation = Guid.NewGuid().ToString("N");
                var history = new List<Message> { new("system", prompt) };
                var completed = 0;
                long? firstMs = null;
                string? error = null;
                foreach (var input in item.Messages)
                {
                    history.Add(new("user", input));
                    progress?.Invoke($"{item.Id}, powtórzenie {repetition}/{item.Repetitions}, tura {completed + 1}/{item.Messages.Length}");
                    try
                    {
                        var reply = await send(new(item.Id, repetition, completed + 1,
                            conversation, history.ToArray()));
                        if (string.IsNullOrWhiteSpace(reply.Content))
                            throw new InvalidDataException("Pusta odpowiedź modelu.");
                        firstMs ??= reply.ElapsedMs;
                        history.Add(new("assistant", reply.Content));
                        completed++;
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
                    {
                        error = ex.Message;
                        progress?.Invoke($"Błąd techniczny: {error}. Pomijam resztę tej rozmowy.");
                        break;
                    }
                }
                results.Add(new(item.Id, repetition, conversation,
                    error is null ? "completed" : "technical_error", completed, firstMs, error));
                await checkpoint(results.ToArray());
            }
        }
        return results;
    }
}
