using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class MappingBuilder
    {
        public static string BuildFromSource(
            string dtoTypeName,
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties)
        {
            var sb = new StringBuilder();

            sb.AppendLine("    public static " + dtoTypeName + " FromSource(" + sourceTypeFullName + " source)");
            sb.AppendLine("    {");
            sb.AppendLine("        return new " + dtoTypeName);
            sb.AppendLine("        {");

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Length - 1 ? "," : "";

                if (prop.MappingKind == MappingKind.Collection)
                {
                    var collSourceName = prop.SourcePropertyName;
                    var toMethod = prop.IsArray ? ".ToArray()" : ".ToList()";
                    string collExpr;
                    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
                    {
                        collExpr = "source." + collSourceName + ".Select(x => " + prop.NestedDtoTypeFullName + ".FromSource(x))" + toMethod;
                    }
                    else
                    {
                        collExpr = "source." + collSourceName + toMethod;
                    }

                    string assignment;
                    if (prop.IsNullable)
                    {
                        assignment = prop.Name + " = source." + collSourceName + " != null ? " + collExpr + " : null";
                    }
                    else
                    {
                        assignment = prop.Name + " = " + collExpr;
                    }
                    sb.AppendLine("            " + assignment + comma);
                }
                else if (prop.MappingKind == MappingKind.Nested)
                {
                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = source." + prop.Name + " != null ? " + prop.NestedDtoTypeFullName + ".FromSource(source." + prop.Name + ") : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = " + prop.NestedDtoTypeFullName + ".FromSource(source." + prop.Name + ")" + comma);
                    }
                }
                else
                {
                    var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                    sb.AppendLine("            " + prop.Name + " = source." + sourceName + comma);
                }
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }

        public static string BuildToSource(
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties)
        {
            var sb = new StringBuilder();

            sb.AppendLine("    public " + sourceTypeFullName + " ToSource()");
            sb.AppendLine("    {");
            sb.AppendLine("        return new " + sourceTypeFullName);
            sb.AppendLine("        {");

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Length - 1 ? "," : "";

                if (prop.MappingKind == MappingKind.Collection)
                {
                    var collTarget = prop.SourcePropertyName;
                    var toMethodR = prop.IsArray ? ".ToArray()" : ".ToList()";
                    string collExprR;
                    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
                    {
                        collExprR = "this." + prop.Name + ".Select(x => x.ToSource())" + toMethodR;
                    }
                    else
                    {
                        collExprR = "this." + prop.Name + toMethodR;
                    }

                    string assignment;
                    if (prop.IsNullable)
                    {
                        assignment = collTarget + " = this." + prop.Name + " != null ? " + collExprR + " : null";
                    }
                    else
                    {
                        assignment = collTarget + " = " + collExprR;
                    }
                    sb.AppendLine("            " + assignment + comma);
                }
                else if (prop.MappingKind == MappingKind.Nested)
                {
                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = this." + prop.Name + " != null ? this." + prop.Name + ".ToSource() : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = this." + prop.Name + ".ToSource()" + comma);
                    }
                }
                else
                {
                    var targetName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                    sb.AppendLine("            " + targetName + " = this." + prop.Name + comma);
                }
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }
    }
}
