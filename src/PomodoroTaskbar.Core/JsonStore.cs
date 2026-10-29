using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace PomodoroTaskbar.Core;

public sealed class JsonStore(string directory)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    public AppSettings LoadSettings() => Read("settings.json", () => new AppSettings(), value => value.Validate());
    public void SaveSettings(AppSettings value) { value.Validate(); Write("settings.json", value); }
    public SessionSnapshot? LoadSession() => Read<SessionSnapshot?>("session.json", () => null, value =>
    {
        if (value is not null) new PomodoroEngine(new()).Restore(value);
    });
    public void SaveSession(SessionSnapshot value) { new PomodoroEngine(new()).Restore(value); Write("session.json", value); }
    private T Read<T>(string name, Func<T> fallback, Action<T> validate)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path)) return fallback();
        try
        {
            var json = File.ReadAllText(path);
            if (name == "settings.json") json = MigrateSettings(json);
            var value = JsonSerializer.Deserialize<T>(json, Options);
            if (value is null) throw new InvalidDataException("Empty document.");
            validate(value); return value;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            try { File.Copy(path, path + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), true); } catch (Exception copyError) when (copyError is IOException or UnauthorizedAccessException) { }
            return fallback();
        }
    }
    private static string MigrateSettings(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Invalid settings document.");
        var version = 1;
        if (root["schemaVersion"] is { } schema && (schema is not JsonValue schemaValue || !schemaValue.TryGetValue<int>(out version)))
            throw new InvalidDataException("Invalid settings schema.");
        if (version is not (1 or 2)) throw new InvalidDataException("Unsupported settings schema.");
        if (version == 1)
        {
            if (root["theme"] is JsonObject legacy)
            {
                var colors = new JsonObject();
                foreach (var key in new[] { "focusColor", "shortBreakColor", "longBreakColor" })
                    if (legacy[key] is { } color) colors[key] = color.DeepClone();
                root["colors"] = colors;
            }
            root.Remove("theme");
            root["schemaVersion"] = 2;
        }
        return root.ToJsonString();
    }
    private void Write<T>(string name, T value)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
        File.Move(temp, path, true);
    }
}
