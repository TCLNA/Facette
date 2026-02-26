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
                var defaultValue = prop.IsValueType ? "" : " = default!;";
                sb.AppendLine("    public " + prop.TypeFullName + " " + prop.Name + " { get; init; }" + defaultValue);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
