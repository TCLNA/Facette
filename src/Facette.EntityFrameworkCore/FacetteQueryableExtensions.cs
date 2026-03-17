using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Facette.EntityFrameworkCore;

/// <summary>
/// Extension methods for integrating Facette DTO projections and filtering with EF Core queries.
/// </summary>
public static class FacetteQueryableExtensions
{
    /// <summary>
    /// Projects an <see cref="IQueryable{TSource}"/> to DTOs using the provided Facette projection expression.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TDto">The target DTO type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="projection">The Facette-generated projection expression (e.g. <c>MyDto.Projection</c>).</param>
    public static IQueryable<TDto> ProjectToFacette<TSource, TDto>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TDto>> projection)
    {
        return query.Select(projection);
    }

    /// <summary>
    /// Filters an <see cref="IQueryable{TSource}"/> using a DTO-level predicate that is rewritten
    /// to a source-level predicate via Facette's <c>MapExpression</c>.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TDto">The DTO type used in the predicate.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="predicate">A predicate expressed in terms of the DTO.</param>
    /// <param name="mapExpression">The Facette-generated expression mapper (e.g. <c>MyDto.MapExpression</c>).</param>
    public static IQueryable<TSource> WhereFacette<TSource, TDto>(
        this IQueryable<TSource> query,
        Expression<Func<TDto, bool>> predicate,
        Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression)
    {
        return query.Where(mapExpression(predicate));
    }

    /// <summary>
    /// Projects a <see cref="DbSet{TSource}"/> to DTOs using the provided Facette projection expression.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TDto">The target DTO type.</typeparam>
    /// <param name="dbSet">The EF Core DbSet.</param>
    /// <param name="projection">The Facette-generated projection expression (e.g. <c>MyDto.Projection</c>).</param>
    public static IQueryable<TDto> ProjectToFacette<TSource, TDto>(
        this DbSet<TSource> dbSet,
        Expression<Func<TSource, TDto>> projection) where TSource : class
    {
        return dbSet.Select(projection);
    }

    /// <summary>
    /// Filters a <see cref="DbSet{TSource}"/> using a DTO-level predicate that is rewritten
    /// to a source-level predicate via Facette's <c>MapExpression</c>.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TDto">The DTO type used in the predicate.</typeparam>
    /// <param name="dbSet">The EF Core DbSet.</param>
    /// <param name="predicate">A predicate expressed in terms of the DTO.</param>
    /// <param name="mapExpression">The Facette-generated expression mapper (e.g. <c>MyDto.MapExpression</c>).</param>
    public static IQueryable<TSource> WhereFacette<TSource, TDto>(
        this DbSet<TSource> dbSet,
        Expression<Func<TDto, bool>> predicate,
        Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression) where TSource : class
    {
        return dbSet.AsQueryable().Where(mapExpression(predicate));
    }
}
