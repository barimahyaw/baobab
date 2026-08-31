using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;
using System.Text.RegularExpressions;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public sealed partial class GhanaCardPersonalIdentificationNumber(string value) : ValueObject
{
    public string Value { get; private set; } = value;

    internal const int Length = 15;

    public static Result Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail(Errors.GhanaCardPersonalIdentificationNumberErrors
                .GhanaCardPersonalIdentificationNumberNullOrEmpty);

        if (value.Length < Length || value.Length > Length)
            return Result.Fail(Errors.GhanaCardPersonalIdentificationNumberErrors
                .GhanaCardPersonalIdentificationNumberTooShortOrTooLong);

        var ghanaCardNumberRegex = GhanaCardPersonalIdentificationNumberValidityExpression();
        if (!ghanaCardNumberRegex.IsMatch(value))
            return Result.Fail(Errors.GhanaCardPersonalIdentificationNumberErrors
                .GhanaCardPersonalIdentificationNumberInvalid);

        return Result.Success();
    }

    public static GhanaCardPersonalIdentificationNumber Create(string value)
        => new(value);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^GHA-\d{9}-\d$")]
    private static partial Regex GhanaCardPersonalIdentificationNumberValidityExpression();

}

