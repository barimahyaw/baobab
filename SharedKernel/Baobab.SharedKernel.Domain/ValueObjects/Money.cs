using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public class Money(string currency, decimal amount) : ValueObject
{
    public decimal Amount { get; private set; } = amount;
    public string Currency { get; private set; } = currency;

    public static Result Validate(string currency, decimal amount)
    {
        if(string.IsNullOrWhiteSpace(currency))
            return Result.Fail(Errors.MoneyErrors.CurrencyNullOrEmpty);
        if(amount <= 0)
            return Result.Fail(Errors.MoneyErrors.InvalidAmount);

        return Result.Success();
    }

    public override string ToString() => $"{Currency} {Amount}";

    public static Money Create(string currency, decimal amount)
        => new(currency, amount);

    // validate top up/ charge money
    public Result ValidateAddSubtract(string currency, decimal amount)
    {
        var validationResult = Validate(currency, amount);
        if (!validationResult.Succeeded) return validationResult;

        if (currency != Currency) return Result.Fail(Errors.MoneyErrors.CurrencyMismatch);

        return Result.Success();
    }

    // top up money
    public Money Add(Money money) => new(Currency, Amount + money.Amount);

    // subtract money
    public Money Subtract(Money money) => new(Currency, Amount - money.Amount);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Currency;
        yield return Amount;
    }
}
