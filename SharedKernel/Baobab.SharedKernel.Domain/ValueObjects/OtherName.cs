using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public class OtherName(string value) : ValueObject
{
    public string Value { get; private set; } = value;

    internal const int MaxLength = 50;

    public static Result Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success();

        if (!string.IsNullOrWhiteSpace(value) && value.Length > 50)
            return Result.Fail(Errors.OtherNameErrors.OtherNameTooLong);

        return Result.Success();
    }

    public static OtherName Create(string value)
        => new(value);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}