using Baobab.SharedKernel.Domain.Enums;
using Baobab.SharedKernel.Domain.Primitives;
using System.Linq.Expressions;

namespace Baobab.SharedKernel.Persistence.Specifications;

public abstract class HeroSpecification<T> : ISpecification<T> where T : Entity
{
    public Expression<Func<T, bool>> Criteria { get; set; } = null!;
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>> OrderBy { get; private set; } = null!;
    public SortDirection SortDirection { get; private set; }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);

    protected virtual void AddInclude(string includeString) => IncludeStrings.Add(includeString);

    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression, SortDirection sortDirection = SortDirection.Descending)
    {
        OrderBy = orderByExpression;
        SortDirection = sortDirection;
    }
}
