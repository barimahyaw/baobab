namespace Baobab.SharedKernel.Domain.Results;

public interface IValidationResult
{
    public static readonly Error ValidationError = new(
        "Validation error",
        "A validation problem occurred.");

    Error[] Messages { get; }
}