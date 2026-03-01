using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Facette.EntityFrameworkCore;

public static class FacetteQueryableExtensions
{
    public static IQueryable<TDto> ProjectToFacette<TSource, TDto>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TDto>> projection)
    {
        return query.Select(projection);
    }

    public static IQueryable<TSource> WhereFacette<TSource, TDto>(
        this IQueryable<TSource> query,
        Expression<Func<TDto, bool>> predicate,
        Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression)
    {
        return query.Where(mapExpression(predicate));
    }

    public static IQueryable<TDto> ProjectToFacette<TSource, TDto>(
        this DbSet<TSource> dbSet,
        Expression<Func<TSource, TDto>> projection) where TSource : class
    {
        return dbSet.Select(projection);
    }

    public static IQueryable<TSource> WhereFacette<TSource, TDto>(
        this DbSet<TSource> dbSet,
        Expression<Func<TDto, bool>> predicate,
        Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression) where TSource : class
    {
        return dbSet.AsQueryable().Where(mapExpression(predicate));
    }
}
