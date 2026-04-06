namespace Haystack.Models;

public class CustomField
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}
