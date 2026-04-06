using System.Text;
using System.Xml;
using System.Xml.Linq;
using IIS.Api.Models;

namespace IIS.Api.Services;

public static class TaskXmlBuilder
{
    public static string BuildTasksDocument(IEnumerable<TaskDto> tasks)
    {
        var taskElements = tasks.Select(t =>
        {
            var children = new List<XObject>();
            if (t.Id != null)
                children.Add(new XElement("id", t.Id));
            children.Add(new XElement("name", t.Name ?? ""));
            children.Add(new XElement("description", t.Description ?? ""));
            children.Add(new XElement("completed", t.Completed));
            if (t.CreatedAt.HasValue)
                children.Add(new XElement("createdAt", XmlConvert.ToString(t.CreatedAt.Value.UtcDateTime, XmlDateTimeSerializationMode.Utc)));
            return new XElement("Task", children);
        });

        var root = new XElement("Tasks", taskElements);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var sb = new StringBuilder();
        using var writer = new Utf8StringWriter(sb);
        doc.Save(writer);
        return sb.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(StringBuilder sb) : base(sb)
        {
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
