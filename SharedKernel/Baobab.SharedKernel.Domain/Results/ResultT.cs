namespace Baobab.SharedKernel.Domain.Results;

public record ResultT<T> : Result, IResult<T>
{
    protected ResultT(bool succeeded, params Error[] errors)
    {
        Succeeded = succeeded;
        Messages = errors;
    }

    protected ResultT() { }

    public T Data { get; private init; } = default!;

    public new static ResultT<T> Fail()
        => new();

    public new static ResultT<T> Fail(params Error[] errors) =>
        new()
        {
            Messages = errors
        };

    public new static Task<ResultT<T>> FailAsync()
        => Task.FromResult(Fail());

    public new static Task<ResultT<T>> FailAsync(params Error[] errors)
        => Task.FromResult(Fail(errors));

    public new static ResultT<T> Success() =>
        new()
        {
            Succeeded = true
        };

    public new static ResultT<T> Success(params Error[] messages)
        => new()
        {
            Succeeded = true,
            Messages = messages
        };

    public static ResultT<T> Success(T data)
        => new()
        {
            Succeeded = true,
            Data = data
        };

    public static ResultT<T> Success(T data, params Error[] messages)
        => new()
        {
            Succeeded = true,
            Data = data,
            Messages = messages
        };

    public new static Task<ResultT<T>> SuccessAsync()
        => Task.FromResult(Success());

    public new static Task<ResultT<T>> SuccessAsync(params Error[] messages)
        => Task.FromResult(Success(messages));

    public static Task<ResultT<T>> SuccessAsync(T data)
        => Task.FromResult(Success(data));
}