using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.Results;

namespace Baobab.SharedKernel.Domain.ValueObjects;

public class UserId(Guid id) : ValueObject
{
    public Guid Id { get; private set; } = id;

    public static Result Validate(Guid id)
    {
        if (id == Guid.Empty || id == default)
            return Result.Fail(Errors.UserErrors.UserIdEmpty);

        return Result.Success();
    }

    public static UserId Create(Guid id)
        => new(id);

    public static implicit operator Guid(UserId self) => self.Id;
    public static implicit operator UserId(Guid id) => new(id);

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;
    }
}
