namespace IIS.Api.Models;

public class ValidationErrorResponse
{
    public List<string> Errors { get; set; } = new();
}
