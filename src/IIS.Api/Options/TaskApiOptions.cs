namespace IIS.Api.Options;

public class TaskApiOptions
{
    public const string SectionName = "TaskApi";

    /// <summary>Public MockAPI or Custom EF-backed API.</summary>
    public string Provider { get; set; } = "Custom";

    public string PublicBaseUrl { get; set; } = "https://69be01bb17c3d7d97790ff3b.mockapi.io";
}
