using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class PropertyBuilder
    {
        public static string Build(ImmutableArray<PropertyModel> properties)
        {
            var sb = new StringBuilder();

            foreach (var prop in properties)
            {
                // Skip Custom-mapped properties — they are user-declared on the target type
                if (prop.MappingKind == MappingKind.Custom)
                {
                    continue;
                }

                var defaultValue = prop.IsValueType ? "" : " = default!;";
                sb.AppendLine("    public " + prop.TypeFullName + " " + prop.Name + " { get; init; }" + defaultValue);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
