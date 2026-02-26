using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class ProjectionBuilder
    {
        public static string Build(
            string dtoTypeName,
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties)
        {
            var sb = new StringBuilder();

            sb.AppendLine("    public static System.Linq.Expressions.Expression<System.Func<" + sourceTypeFullName + ", " + dtoTypeName + ">> Projection =>");
            sb.AppendLine("        source => new " + dtoTypeName);
            sb.AppendLine("        {");

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Length - 1 ? "," : "";
                sb.AppendLine("            " + prop.Name + " = source." + prop.Name + comma);
            }

            sb.AppendLine("        };");

            return sb.ToString().TrimEnd();
        }
    }
}
