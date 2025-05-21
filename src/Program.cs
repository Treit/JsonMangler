using System.Text.Json;

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run <path-to-json-file> <nodeNameToFilterOut1> [nodeNameToFilterOut2] [nodeNameToFilterOutN]");
    return;
}

var inputPath = args[0];
var fi = new FileInfo(inputPath);
var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fi.Name);
var outputPath = Path.Combine(fi.DirectoryName!, $"{fileNameWithoutExtension}.filtered.json");

var nodesToFilterOut = new HashSet<string>(args.Skip(1), StringComparer.OrdinalIgnoreCase);

Console.WriteLine($"Reading JSON from: {inputPath}");
Console.WriteLine($"Writing filtered JSON (removing {nodesToFilterOut.Count} node types) to: {outputPath}");

try
{
    using FileStream inputStream = File.OpenRead(inputPath);
    using JsonDocument document = JsonDocument.Parse(inputStream);
    using FileStream outputStream = File.Create(outputPath);
    using Utf8JsonWriter writer = new(outputStream, new JsonWriterOptions
    {
        Indented = true
    });

    var removedNodesCount = ProcessElement(document.RootElement, writer, nodesToFilterOut);
    writer.Flush();

    Console.WriteLine($"JSON document recreated successfully with {removedNodesCount} nodes removed.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex}");
}

static int ProcessElement(JsonElement element, Utf8JsonWriter writer, HashSet<string> nodesToFilterOut)
{
    var removedCount = 0;

    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            writer.WriteStartObject();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (nodesToFilterOut.Contains(property.Name))
                {
                    removedCount++;
                    continue;
                }

                writer.WritePropertyName(property.Name);
                removedCount += ProcessElement(property.Value, writer, nodesToFilterOut);
            }
            writer.WriteEndObject();
            break;        case JsonValueKind.Array:
            writer.WriteStartArray();
            foreach (JsonElement arrayElement in element.EnumerateArray())
            {
                removedCount += ProcessElement(arrayElement, writer, nodesToFilterOut);
            }
            writer.WriteEndArray();
            break;

        case JsonValueKind.String:
            writer.WriteStringValue(element.GetString());
            break;

        case JsonValueKind.Number:
            if (element.TryGetInt64(out long intValue))
            {
                writer.WriteNumberValue(intValue);
            }
            else
            {
                writer.WriteNumberValue(element.GetDouble());
            }
            break;

        case JsonValueKind.True:
            writer.WriteBooleanValue(true);
            break;

        case JsonValueKind.False:
            writer.WriteBooleanValue(false);
            break;        case JsonValueKind.Null:
            writer.WriteNullValue();
            break;
    }

    return removedCount;
}