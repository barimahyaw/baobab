namespace Baobab.SharedKernel.Domain.Results;

public record PaginatedResult<T> : Result
{
    public PaginatedResult(List<T> data)
        => Data = data;

    public List<T> Data { get; private set; }

    internal PaginatedResult(bool succeeded, List<T> data = default!, Error[] errors = null!, int count = 0, int page = 1, int pageSize = 10)
    {
        Data = data;
        Messages = errors;
        CurrentPage = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
    }

    public static PaginatedResult<T> Failure(params Error[] errors)
        => new(false, default!, errors);

    public static PaginatedResult<T> Success(List<T> data, int count, int page, int pageSize)
        => new(true, data, null!, count, page, pageSize);

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }
    public int PageSize { get; private set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
}
