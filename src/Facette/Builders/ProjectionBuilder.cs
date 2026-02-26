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

                if (prop.MappingKind == MappingKind.Nested)
                {
                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = source." + prop.Name + " != null ? new " + prop.NestedDtoTypeFullName);
                        sb.AppendLine("            {");
                        for (int j = 0; j < prop.NestedProperties.Length; j++)
                        {
                            var nested = prop.NestedProperties[j];
                            var nestedComma = j < prop.NestedProperties.Length - 1 ? "," : "";
                            sb.AppendLine("                " + nested.Name + " = source." + prop.Name + "." + nested.Name + nestedComma);
                        }
                        sb.AppendLine("            } : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = new " + prop.NestedDtoTypeFullName);
                        sb.AppendLine("            {");
                        for (int j = 0; j < prop.NestedProperties.Length; j++)
                        {
                            var nested = prop.NestedProperties[j];
                            var nestedComma = j < prop.NestedProperties.Length - 1 ? "," : "";
                            sb.AppendLine("                " + nested.Name + " = source." + prop.Name + "." + nested.Name + nestedComma);
                        }
                        sb.AppendLine("            }" + comma);
                    }
                }
                else
                {
                    var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                    sb.AppendLine("            " + prop.Name + " = source." + sourceName + comma);
                }
            }

            sb.AppendLine("        };");

            return sb.ToString().TrimEnd();
        }
    }
}
