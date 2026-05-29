namespace AppModules.Sample.Domain;

public sealed class SampleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
