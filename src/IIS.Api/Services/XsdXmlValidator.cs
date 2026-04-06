using System.Xml;
using System.Xml.Schema;

namespace IIS.Api.Services;

public class XsdXmlValidator(IWebHostEnvironment env)
{
    private readonly Lazy<XmlSchemaSet> _schemas = new(() =>
    {
        var set = new XmlSchemaSet();
        var path = System.IO.Path.Combine(env.ContentRootPath, "Schemas", "tasks.xsd");
        using var reader = XmlReader.Create(path);
        set.Add(null, reader);
        set.Compile();
        return set;
    });

    public IReadOnlyList<string> Validate(string xml)
    {
        var errors = new List<string>();
        var settings = CreateSettings(errors);

        try
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, settings);
            ReadToEnd(reader);
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parse error: {ex.Message}");
        }

        return errors;
    }

    public IReadOnlyList<string> ValidateFile(string xmlPath)
    {
        var errors = new List<string>();
        var settings = CreateSettings(errors);

        try
        {
            using var reader = XmlReader.Create(xmlPath, settings);
            ReadToEnd(reader);
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parse error: {ex.Message}");
        }

        return errors;
    }

    private XmlReaderSettings CreateSettings(List<string> errors)
    {
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = _schemas.Value };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message} (Line {e.Exception?.LineNumber})");
        };
        return settings;
    }

    private static void ReadToEnd(XmlReader reader)
    {
        while (reader.Read())
        {
        }
    }
}
