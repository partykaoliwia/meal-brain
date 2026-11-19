using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class BatchOllamaClient(HttpClient http, string model, bool? think, object options,
    JsonSerializerOptions serializer, Func<object, Task> log)
{
    public async Task<ChatReply> SendAsync(SuiteTurn turn)
    {
        // Do API przekazujemy tylko historię wiadomości, nigdy plik kryteriów ani obiekt zestawu.
        var request = new { model, messages = turn.Messages, stream = false, think, options, keep_alive = "10m" };
        var startedAt = DateTimeOffset.UtcNow;
        var watch = Stopwatch.StartNew();
        string? raw = null;
        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, serializer);
            raw = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Odpowiedź API nie jest obiektem JSON.");
            if (!root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object ||
                !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Brak tekstowej odpowiedzi w message.content.");
            var answer = content.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(answer))
                throw new InvalidDataException("Pusta odpowiedź końcowa.");
            if (!root.TryGetProperty("done", out var done) || done.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("done_reason", out var reason) || reason.ValueKind != JsonValueKind.String || reason.GetString() != "stop")
                throw new InvalidDataException("Odpowiedź nie jest kompletna (done/done_reason). Nie oceniamy jej jako pełnego importu.");
            watch.Stop();
            await log(new { kind = turn.CaseId == "warmup" ? "warmup" : "chat", case_id = turn.CaseId,
                repetition = turn.Repetition, turn = turn.Turn, conversation_id = turn.ConversationId,
                started_at_utc = startedAt, elapsed_ms = watch.ElapsedMilliseconds, request, response = root });
            return new(answer, watch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            watch.Stop();
            await log(new { kind = "error", case_id = turn.CaseId, repetition = turn.Repetition,
                turn = turn.Turn, conversation_id = turn.ConversationId, started_at_utc = startedAt,
                elapsed_ms = watch.ElapsedMilliseconds, request, raw_response = raw, error = ex.Message });
            throw;
        }
    }
}
