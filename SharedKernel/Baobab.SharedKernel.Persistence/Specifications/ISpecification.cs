using Baobab.SharedKernel.Domain.Enums;
using Baobab.SharedKernel.Domain.Primitives;
using System.Linq.Expressions;

namespace Baobab.SharedKernel.Persistence.Specifications;

public interface ISpecification<T> where T : Entity
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>> OrderBy { get; }
    SortDirection SortDirection { get; }
}