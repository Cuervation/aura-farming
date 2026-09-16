using System.IO.Pipes;
using System.Text;
using System.Text.Json;

// Thin .NET 8 client. Unity behavior belongs exclusively to AuraWindowBridgeServer.
var arguments = args.ToList();
var command = arguments.FirstOrDefault(x => !x.Contains('='));
if (string.IsNullOrWhiteSpace(command))
{
    Console.WriteLine("{\"success\":false,\"error\":\"Usage: AuraWindowBridge <command> [key=value]\"}");
    return 2;
}
var values = arguments.Where(x => x.Contains('='))
    .Select(x => x.Split('=', 2))
    .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);
var timeout = values.TryGetValue("timeoutMs", out var rawTimeout) && int.TryParse(rawTimeout, out var parsedTimeout) ? Math.Clamp(parsedTimeout, 100, 120000) : 10000;
values.Remove("timeoutMs");
var request = new Dictionary<string, object?> { ["command"] = command, ["timeoutMs"] = timeout };
foreach (var pair in values) request[pair.Key] = pair.Value;
try
{
    using var pipe = new NamedPipeClientStream(".", "AuraWindowBridge.v1", PipeDirection.InOut, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(timeout);
    await using (var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
        await writer.WriteLineAsync(JsonSerializer.Serialize(request));
    using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
    var line = await reader.ReadLineAsync();
    if (string.IsNullOrEmpty(line)) throw new IOException("Unity closed the pipe without a response.");
    using var envelope = JsonDocument.Parse(line);
    var root = envelope.RootElement;
    var output = new Dictionary<string, object?>
    {
        ["success"] = root.TryGetProperty("success", out var success) && success.GetBoolean(),
        ["command"] = command,
        ["error"] = root.TryGetProperty("error", out var error) ? error.GetString() : null
    };
    var dataJson = root.TryGetProperty("dataJson", out var data) ? data.GetString() : null;
    output["data"] = string.IsNullOrWhiteSpace(dataJson) ? null : JsonSerializer.Deserialize<JsonElement>(dataJson);
    Console.WriteLine(JsonSerializer.Serialize(output));
    return output["success"] is true ? 0 : 1;
}
catch (Exception exception)
{
    Console.WriteLine(JsonSerializer.Serialize(new { success = false, command, error = exception.Message, data = (object?)null }));
    return 1;
}


