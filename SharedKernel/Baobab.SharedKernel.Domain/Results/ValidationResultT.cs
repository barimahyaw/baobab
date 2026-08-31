namespace Baobab.SharedKernel.Domain.Results;

public record ValidationResultT<T> : ResultT<T>, IValidationResult
{
    private ValidationResultT(Error[] errors) =>
        Messages = errors;

    public static ValidationResultT<T> WithErrors(Error[] errors) =>
        new(errors);
}