namespace Baobab.SharedKernel.Domain.Results;

public interface IResult
{
    bool Succeeded { get; }
    Error[] Messages { get; }
}

public interface IResult<out T> : IResult
{
    T Data { get; }
}