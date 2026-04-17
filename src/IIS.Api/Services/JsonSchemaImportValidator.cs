using System.Text.Json;
using Json.Schema;

namespace IIS.Api.Services;

public class JsonSchemaImportValidator(IWebHostEnvironment env)
{
    private readonly Lazy<JsonSchema> _schema = new(() =>
    {
        var path = System.IO.Path.Combine(env.ContentRootPath, "Schemas", "tasks-import.schema.json");
        var text = File.ReadAllText(path);
        return JsonSchema.FromText(text);
    });

    public IReadOnlyList<string> Validate(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return new[] { $"Invalid JSON: {ex.Message}" };
        }

        using (doc)
        {
            JsonSchema schema;
            try
            {
                schema = _schema.Value;
            }
            catch (Exception ex)
            {
                return new[] { $"Schema initialization failed: {ex.Message}" };
            }

            EvaluationResults result;
            try
            {
                result = schema.Evaluate(doc.RootElement, new EvaluationOptions
                {
                    OutputFormat = OutputFormat.List
                });
            }
            catch (Exception ex)
            {
                return new[] { $"Schema evaluation failed: {ex.Message}" };
            }

            if (result.IsValid)
                return Array.Empty<string>();

            var list = new List<string>();
            Walk(result, list);
            return list;
        }
    }

    private static void Walk(EvaluationResults r, List<string> list)
    {
        if (r.Errors != null)
        {
            foreach (var kv in r.Errors)
                list.Add($"{r.InstanceLocation}: {kv.Value}");
        }

        foreach (var d in r.Details ?? new List<EvaluationResults>())
            Walk(d, list);
    }
}
