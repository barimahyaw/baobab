namespace Baobab.SharedKernel.Domain.Results;

public record Result : IResult
{
    public bool Succeeded { get; protected init; }
    public Error[] Messages { get; protected init; } = default!;
    protected Result() { }

    protected Result(bool succeeded, params Error[] messages)
    {
        Succeeded = succeeded;
        Messages = messages;
    }

    public static Result Fail() => new(false);

    public static Result Fail(params Error[] errors) => new(false, errors);

    public static Task<Result> FailAsync() => Task.FromResult(Fail());

    public static Task<Result> FailAsync(params Error[] errors) => Task.FromResult(Fail(errors));

    public static Result Success() => new(true);

    public static Result Success(params Error[] messages) => new(true, messages);

    public static Task<Result> SuccessAsync() => Task.FromResult(Success());

    public static Task<Result> SuccessAsync(params Error[] messages) => Task.FromResult(Success(messages));
}