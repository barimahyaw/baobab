namespace Baobab.SharedKernel.Domain.Requests;

public record PaginatedRequest
{
    public string? SearchTerm { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public string? SortLabel { get; set; }
    public bool IsDownloadRequest { get; set; }
}