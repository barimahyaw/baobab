using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;
public partial class DateOfBirth(DateOnly value) : ValueObject
{
    public DateOnly Value { get; private set; } = value;

    public static DateOfBirth Create(DateOnly value)
        => new(value);

    public static Result Validate(DateOnly value, bool isPatient = false)
    {
        if (value == default)
            return Result.Fail(Errors.DateOfBirth.DateOfBirthNullOrEmpty);

        if (value < DateOnly.MinValue)
            return Result.Fail(Errors.DateOfBirth.DateOfBirthInvalidFormat);

        if (value > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Fail(Errors.DateOfBirth.DateOfBirthInFuture);

        // validate age not to be less than 18
        var age = CalculateAge(value);
        if (!isPatient && age < 18)
            return Result.Fail(Errors.DateOfBirth.DateOfBirthLessThan18Years);

        return Result.Success();
    }

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        int age = currentDate.Year - birthDate.Year;

        // Adjust age if the birthday hasn't occurred yet this year
        if (currentDate < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}