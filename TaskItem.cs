public sealed class TaskItem
{
    public string Status { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string ImageFile { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class ExtractResult
{
    public string ProductId { get; init; } = "";
    public string ProductName { get; init; } = "";
    public string SellerId { get; init; } = "";
    public string ShopId { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string OutputFile { get; init; } = "";
    public bool UsedFallback { get; init; }
}
