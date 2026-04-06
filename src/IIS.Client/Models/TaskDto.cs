namespace IIS.Client.Models;

public class TaskDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Completed { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
