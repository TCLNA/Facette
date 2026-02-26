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

                if (prop.MappingKind == MappingKind.Collection)
                {
                    var cSourceName = prop.SourcePropertyName;
                    var cToMethod = prop.IsArray ? ".ToArray()" : ".ToList()";
                    string cExpr;
                    if (prop.NestedProperties.Length > 0)
                    {
                        var selectBody = new StringBuilder();
                        selectBody.Append("new " + prop.NestedDtoTypeFullName + " { ");
                        for (int j = 0; j < prop.NestedProperties.Length; j++)
                        {
                            var np = prop.NestedProperties[j];
                            var nSourceName = np.MappingKind == MappingKind.Custom ? np.SourcePropertyName : np.Name;
                            var nComma = j < prop.NestedProperties.Length - 1 ? ", " : "";
                            selectBody.Append(np.Name + " = x." + nSourceName + nComma);
                        }
                        selectBody.Append(" }");
                        cExpr = "source." + cSourceName + ".Select(x => " + selectBody + ")" + cToMethod;
                    }
                    else
                    {
                        cExpr = "source." + cSourceName + cToMethod;
                    }

                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = source." + cSourceName + " != null ? " + cExpr + " : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = " + cExpr + comma);
                    }
                }
                else if (prop.MappingKind == MappingKind.Nested)
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
