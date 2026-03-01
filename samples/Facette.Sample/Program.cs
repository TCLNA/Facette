using System.Linq.Expressions;
using Facette.Sample.Dtos;
using Facette.Sample.Models;

// ── Sample data ─────────────────────────────────────────────────────────────

var address = new Address
{
    Street = "123 Main St",
    City = "Portland",
    State = "OR",
    ZipCode = "97201"
};

var employee = new Employee
{
    Id = 1,
    FirstName = "Alice",
    LastName = "Johnson",
    Email = "alice@example.com",
    Salary = 95_000m,
    HireDate = new DateTime(2021, 3, 15),
    Department = Department.Engineering,
    HomeAddress = address,
    IsActive = true,
    SocialSecurityNumber = "123-45-6789",
    Notes = "Team lead"
};

var profile = new EmployeeProfile
{
    Bio = "Senior engineer with 10 years of experience",
    PhotoUrl = "https://example.com/photos/alice.jpg"
};

var product = new Product
{
    Id = 1,
    Name = "Widget",
    Description = "A fine widget",
    Price = 9.99m,
    InternalSku = "WDG-001",
    CreatedAt = DateTime.UtcNow,
    Category = new Category { Id = 10, Name = "Tools" },
    Reviews = new List<Review>
    {
        new() { Id = 1, Author = "Alice", Text = "Great product!", Rating = 5 },
        new() { Id = 2, Author = "Bob", Text = "Decent quality", Rating = 3 }
    }
};

var order = new Order
{
    Id = 100,
    OrderDate = new DateTime(2025, 1, 20),
    CustomerName = "Bob Smith",
    Status = OrderStatus.Shipped,
    ShippingAddress = address,
    Items = new List<OrderItem>
    {
        new() { ProductName = "Widget", UnitPrice = 9.99m, Quantity = 2 },
        new() { ProductName = "Gadget", UnitPrice = 24.99m, Quantity = 1 }
    }
};


// ─────────────────────────────────────────────────────────────────────────────
// 1. BASIC MAPPING — FromSource, ToSource, nested objects & collections
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 1. Basic Mapping (nested objects + collections) ═══");

var productDto = ProductDto.FromSource(product);
Console.WriteLine($"  Product: {productDto.Name} - {productDto.Price:C}");
Console.WriteLine($"  Category: {productDto.Category.Name}");
Console.WriteLine($"  Reviews: {productDto.Reviews.Count}");
foreach (var r in productDto.Reviews)
    Console.WriteLine($"    - {r.Author}: \"{r.Text}\" ({r.Rating}/5)");

var roundTripped = productDto.ToSource();
Console.WriteLine($"  Round-trip: {roundTripped.Name}, {roundTripped.Reviews.Count} reviews");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 2. EXCLUSION & SENSITIVE FIELDS — SocialSecurityNumber excluded from DTO
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 2. Field Exclusion ═══");

var empDto = EmployeeDto.FromSource(employee);
Console.WriteLine($"  Employee: {empDto.FirstName} {empDto.LastName}");
Console.WriteLine($"  SSN property exists? {typeof(EmployeeDto).GetProperty("SocialSecurityNumber") != null}");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 3. ENUM → STRING CONVERSION — Department enum mapped to string
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 3. Enum → String Conversion ═══");

Console.WriteLine($"  Source: Department.{employee.Department}");
Console.WriteLine($"  DTO:    DepartmentName = \"{empDto.DepartmentName}\"");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 4. VALUE CONVERSION — DateTime → string with custom converter
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 4. Value Conversion (DateTime → string) ═══");

Console.WriteLine($"  Source: HireDate = {employee.HireDate:d}");
Console.WriteLine($"  DTO:    HiredOn  = \"{empDto.HiredOn}\"");

var backToEmp = empDto.ToSource();
Console.WriteLine($"  Round-trip: HireDate = {backToEmp.HireDate:d}");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 5. AFTERMAP HOOK — Computed FullName property
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 5. AfterMap Hook (computed FullName) ═══");

Console.WriteLine($"  FullName = \"{empDto.FullName}\"");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 6. COPY ATTRIBUTES — Data annotations copied from source
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 6. Copy Attributes from Source ═══");

var attrs = typeof(EmployeeDto).GetProperty("FirstName")!
    .GetCustomAttributes(false);
foreach (var attr in attrs)
    Console.WriteLine($"  [EmployeeDto.FirstName] has: {attr.GetType().Name}");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 7. CONVENTION FLATTENING — HomeAddress.City → HomeAddressCity
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 7. Convention Flattening & MapFrom ═══");

var summary = EmployeeSummaryDto.FromSource(employee);
Console.WriteLine($"  HomeAddressCity = \"{summary.HomeAddressCity}\"  (flattened from HomeAddress.City)");
Console.WriteLine($"  State           = \"{summary.State}\"   (MapFrom \"HomeAddress.State\")");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 8. INCLUDE FILTER — Only specified properties are generated
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 8. Include Filter ═══");

var summaryProps = typeof(EmployeeSummaryDto).GetProperties()
    .Select(p => p.Name)
    .OrderBy(n => n);
Console.WriteLine($"  EmployeeSummaryDto properties: {string.Join(", ", summaryProps)}");
Console.WriteLine($"  (Salary, HireDate, Department etc. are excluded)");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 9. CRUD PRESETS — Create (no Id) and Read (no ToSource)
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 9. CRUD Presets ═══");

var createDto = CreateEmployeeDto.FromSource(employee);
Console.WriteLine($"  CreateEmployeeDto — Id property? {typeof(CreateEmployeeDto).GetProperty("Id") != null}");
Console.WriteLine($"  CreateEmployeeDto — FirstName = \"{createDto.FirstName}\"");

var readDto = ReadEmployeeDto.FromSource(employee);
Console.WriteLine($"  ReadEmployeeDto  — Has ToSource? {typeof(ReadEmployeeDto).GetMethod("ToSource") != null}");
Console.WriteLine($"  ReadEmployeeDto  — Id = {readDto.Id}, Name = \"{readDto.FirstName} {readDto.LastName}\"");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 10. NULLABLE MODE — All properties nullable (useful for PATCH operations)
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 10. Nullable Mode (AllNullable) ═══");

var nullableDto = NullableEmployeeDto.FromSource(employee);
Console.WriteLine($"  NullableEmployeeDto.Id type: {typeof(NullableEmployeeDto).GetProperty("Id")!.PropertyType.Name}");
Console.WriteLine($"  Mapped: Id = {nullableDto.Id}, FirstName = \"{nullableDto.FirstName}\"");

// Can set everything to null — useful for partial updates
var patchDto = new NullableEmployeeDto
{
    Id = null,
    FirstName = "Updated",
    LastName = null,
    Email = null,
    Salary = null,
    HireDate = null,
    IsActive = null,
    Notes = null
};
Console.WriteLine($"  Patch:  Id = {patchDto.Id?.ToString() ?? "(null)"}, FirstName = \"{patchDto.FirstName}\"");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 11. CONDITIONAL MAPPING — [MapWhen] controls field visibility at runtime
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 11. Conditional Mapping ([MapWhen]) ═══");

ConditionalEmployeeDto.SetIncludeSalary(true);
var withSalary = ConditionalEmployeeDto.FromSource(employee);
Console.WriteLine($"  Salary visible:   {withSalary.Salary:C}");

ConditionalEmployeeDto.SetIncludeSalary(false);
var withoutSalary = ConditionalEmployeeDto.FromSource(employee);
Console.WriteLine($"  Salary hidden:    {withoutSalary.Salary:C}  (default when condition is false)");

ConditionalEmployeeDto.SetIncludeSalary(true); // reset

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 12. MULTI-SOURCE MAPPING — Employee + EmployeeProfile combined
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 12. Multi-Source Mapping ═══");

var detailDto = EmployeeDetailDto.FromSource(employee, profile);
Console.WriteLine($"  Employee:  {detailDto.FirstName} {detailDto.LastName}");
Console.WriteLine($"  ProfileBio:      \"{detailDto.ProfileBio}\"");
Console.WriteLine($"  ProfilePhotoUrl: \"{detailDto.ProfilePhotoUrl}\"");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 13. NESTED COLLECTIONS + ENUM CONVERSION — Order with items
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 13. Nested Collections + Enum Conversion ═══");

var orderDto = OrderDto.FromSource(order);
Console.WriteLine($"  Order #{orderDto.Id}: {orderDto.CustomerName}");
Console.WriteLine($"  Status: \"{orderDto.StatusText}\"  (enum → string)");
Console.WriteLine($"  Ship to: {orderDto.ShippingAddress.City}, {orderDto.ShippingAddress.State}");
Console.WriteLine($"  Items ({orderDto.Items.Count}):");
foreach (var item in orderDto.Items)
    Console.WriteLine($"    - {item.ProductName} x{item.Quantity} @ {item.UnitPrice:C}");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 14. EXPRESSION MAPPING — Rewrite DTO predicates to source queries
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 14. Expression Mapping (query rewriting) ═══");

var orders = new List<Order>
{
    order,
    new() { Id = 101, OrderDate = DateTime.Now, CustomerName = "Carol White",
             Status = OrderStatus.Pending, ShippingAddress = new Address { City = "Seattle", State = "WA" },
             Items = new List<OrderItem> { new() { ProductName = "Gizmo", UnitPrice = 5.99m, Quantity = 3 } } },
    new() { Id = 102, OrderDate = DateTime.Now, CustomerName = "Dave Lee",
             Status = OrderStatus.Delivered, ShippingAddress = new Address { City = "Portland", State = "OR" },
             Items = new List<OrderItem> { new() { ProductName = "Widget", UnitPrice = 9.99m, Quantity = 1 } } }
};

// Write a predicate against the DTO type...
Expression<Func<OrderDto, bool>> dtoPredicate = dto => dto.CustomerName == "Bob Smith";

// ...and MapExpression rewrites it to work against the source type
var sourcePredicate = OrderDto.MapExpression(dtoPredicate);
var matched = orders.AsQueryable().Where(sourcePredicate).ToList();
Console.WriteLine($"  Predicate: dto.CustomerName == \"Bob Smith\"");
Console.WriteLine($"  Matched {matched.Count} order(s): #{matched[0].Id} ({matched[0].CustomerName})");

Console.WriteLine();


// ─────────────────────────────────────────────────────────────────────────────
// 15. LINQ PROJECTION — Select expression for EF Core / IQueryable
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("═══ 15. LINQ Projection ═══");

var projected = orders.AsQueryable()
    .Select(OrderDto.Projection.Compile())
    .Where(d => d.Items.Count > 1)
    .ToList();
Console.WriteLine($"  Projected {orders.Count} orders → filtered to {projected.Count} with >1 item");
foreach (var d in projected)
    Console.WriteLine($"    #{d.Id}: {d.CustomerName} — {d.Items.Count} items, status \"{d.StatusText}\"");

Console.WriteLine();
Console.WriteLine("Facette sample complete!");
