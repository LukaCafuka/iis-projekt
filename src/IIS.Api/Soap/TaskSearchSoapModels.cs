using System.Runtime.Serialization;

namespace IIS.Api.Soap;

[DataContract(Namespace = "http://iis.local/soap/tasks")]
public class TaskSearchSoapResponse
{
    [DataMember]
    public List<TaskSoapItem> Items { get; set; } = new();

    [DataMember]
    public List<string> ValidationErrors { get; set; } = new();
}

[DataContract(Namespace = "http://iis.local/soap/tasks")]
public class TaskSoapItem
{
    [DataMember(Order = 1)]
    public string? Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Description { get; set; }

    [DataMember(Order = 4)]
    public bool Completed { get; set; }

    [DataMember(Order = 5)]
    public string? CreatedAt { get; set; }
}
