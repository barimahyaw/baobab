using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public sealed class PhoneNumber(string code, string value) : ValueObject
{
    public string Value { get; private set; } = value;
    public string CountryCode { get; private set; } = code;

    internal const int MaxLength = 50;
    internal const int MinLength = 6;
    internal const int CountryCodeLength = 4;

    public static Result Validate(string code, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail(Errors.PhoneNumberErrors.PhoneNumberNullOrEmpty);

        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail(Errors.PhoneNumberErrors.PhoneNumberCountryCodeNullOrEmpty);

        if (value.Length < MinLength || value.Length > MaxLength)
            return Result.Fail(Errors.PhoneNumberErrors.PhoneNumberTooShortOrTooLong);

        if (value.Any(c => !char.IsDigit(c)) &&
            (value[0] is not '2' || value[0] is not '5'))
        {
            return Result.Fail(Errors.PhoneNumberErrors.PhoneNumberInvalid);
        }

        // validate country code (Ghana Only). Note: the first character can be a '+' sign
        // TODO: add more countries
        if (code.Length != CountryCodeLength ||
            code.Any(c => !char.IsDigit(c) && c != '+') ||
            code[0] != '+' ||
            code[1] != '2' ||
            code[2] != '3' ||
            code[3] != '3')
        {
            return Result.Fail(Errors.PhoneNumberErrors.PhoneNumberInvalidCountryCode);
        }

        return Result.Success();
    }

    public static PhoneNumber Create(string code, string value)
        => new(code, value);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}