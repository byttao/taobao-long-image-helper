public sealed class TaskItem
{
    public string Status { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string SellerId { get; set; } = "";
    public string ShopId { get; set; } = "";
    public string ImageUrl { get; set; } = "";
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
}

public sealed class ExtractDiagnosticException : InvalidOperationException
{
    public ExtractDiagnosticException(
        string message,
        string diagnosticUrl,
        string productName,
        string sellerId,
        string shopId,
        Exception innerException)
        : base(message, innerException)
    {
        DiagnosticUrl = diagnosticUrl;
        ProductName = productName;
        SellerId = sellerId;
        ShopId = shopId;
    }

    public string DiagnosticUrl { get; }
    public string ProductName { get; }
    public string SellerId { get; }
    public string ShopId { get; }
}
