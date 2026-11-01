using System.Linq.Expressions;

namespace Gocgun.Extensions;

/// <summary>Expression composition.</summary>
public static class ExpressionExtensions
{
    /// <summary>ANDs two predicates and rebinds both to one parameter.</summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="left">The first predicate.</param>
    /// <param name="right">The second predicate.</param>
    /// <returns>The combined predicate.</returns>
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ReplaceParameterVisitor();
        visitor.Add(left.Parameters[0], parameter);
        visitor.Add(right.Parameters[0], parameter);
        var combined = visitor.Visit(Expression.AndAlso(left.Body, right.Body));
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map = [];

        public void Add(ParameterExpression from, ParameterExpression to) => map[from] = to;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (map.TryGetValue(node, out var replacement))
            {
                node = replacement;
            }

            return base.VisitParameter(node);
        }
    }
}
