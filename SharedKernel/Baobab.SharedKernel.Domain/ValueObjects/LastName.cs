using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public sealed class LastName(string value) : ValueObject
{
    public string Value { get; private set; } = value;

    internal const int MaxLength = 50;

    public static Result Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail(Errors.LastNameErrors.LastNameNullOrEmpty);

        if (value.Length > 50)
            return Result.Fail(Errors.LastNameErrors.LastNameTooLong);

        return Result.Success();
    }

    public static LastName Create(string value)
        => new(value);

    public static implicit operator string(LastName lastName) => lastName.Value;

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}