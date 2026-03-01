# EF Core Integration

The `Facette.EntityFrameworkCore` package provides extension methods that integrate Facette's projection and expression mapping with Entity Framework Core queries.

## Installation

```bash
dotnet add package Facette.EntityFrameworkCore
```

## ProjectToFacette

Projects an `IQueryable<TSource>` to `IQueryable<TDto>` using the DTO's generated `Projection` expression:

```csharp
using Facette.EntityFrameworkCore;

var employeeDtos = await dbContext.Employees
    .ProjectToFacette(EmployeeDto.Projection)
    .ToListAsync();
```

This translates to a `SELECT` with only the DTO's properties — no over-fetching. The projection is an expression tree, so EF Core translates it to SQL.

`ProjectToFacette` also works directly on `DbSet<T>`:

```csharp
var dtos = await dbContext.Set<Employee>()
    .ProjectToFacette(EmployeeDto.Projection)
    .Where(dto => dto.IsActive)
    .ToListAsync();
```

## WhereFacette

Filters an `IQueryable<TSource>` using a predicate written against the DTO type:

```csharp
Expression<Func<EmployeeDto, bool>> filter = dto => dto.DepartmentName == "Engineering";

var engineers = await dbContext.Employees
    .WhereFacette(filter, EmployeeDto.MapExpression)
    .ToListAsync();
```

`WhereFacette` calls `MapExpression` internally to rewrite the DTO predicate into a source predicate, then applies it as a `Where` clause. The result is still `IQueryable<TSource>`, so you can chain further queries or project afterward:

```csharp
var engineerDtos = await dbContext.Employees
    .WhereFacette(filter, EmployeeDto.MapExpression)
    .ProjectToFacette(EmployeeDto.Projection)
    .ToListAsync();
```

## No reflection

Unlike some mapping libraries, Facette's EF Core integration uses explicit parameters — `Projection` and `MapExpression` are passed directly. There's no service registration, no runtime type scanning, and no reflection.

## Method signatures

```csharp
// On IQueryable<TSource>
IQueryable<TDto> ProjectToFacette<TSource, TDto>(
    this IQueryable<TSource> query,
    Expression<Func<TSource, TDto>> projection)

IQueryable<TSource> WhereFacette<TSource, TDto>(
    this IQueryable<TSource> query,
    Expression<Func<TDto, bool>> predicate,
    Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression)

// On DbSet<TSource>
IQueryable<TDto> ProjectToFacette<TSource, TDto>(
    this DbSet<TSource> dbSet,
    Expression<Func<TSource, TDto>> projection)

IQueryable<TSource> WhereFacette<TSource, TDto>(
    this DbSet<TSource> dbSet,
    Expression<Func<TDto, bool>> predicate,
    Func<Expression<Func<TDto, bool>>, Expression<Func<TSource, bool>>> mapExpression)
```

## Considerations

- Enum-to-string conversions in projections may not translate to SQL — see [FCT011](./diagnostics.md)
- Custom `Convert` methods in projections must be translatable by your EF Core provider
- Multi-source DTOs (`[AdditionalSource]`) typically disable projection since queries have a single source
