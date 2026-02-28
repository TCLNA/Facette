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

            AppendInitializer(sb, properties, "source", "            ");

            sb.AppendLine("        };");

            return sb.ToString().TrimEnd();
        }

        private static void AppendInitializer(
            StringBuilder sb,
            ImmutableArray<PropertyModel> properties,
            string sourcePrefix,
            string indent)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Length - 1 ? "," : "";

                if (prop.MappingKind == MappingKind.Flattened)
                {
                    var flatExpr = BuildFlattenedExpression(sourcePrefix, prop);
                    sb.AppendLine(indent + prop.Name + " = " + flatExpr + comma);
                }
                else if (prop.MappingKind == MappingKind.Collection)
                {
                    AppendCollectionInitializer(sb, prop, sourcePrefix, indent, comma);
                }
                else if (prop.MappingKind == MappingKind.Nested)
                {
                    AppendNestedInitializer(sb, prop, sourcePrefix, indent, comma);
                }
                else
                {
                    var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                    var convertMethod = prop.ConvertMethod;
                    if (!string.IsNullOrEmpty(convertMethod))
                    {
                        sb.AppendLine(indent + prop.Name + " = " + prop.ConvertContainingType + "." + convertMethod + "(" + sourcePrefix + "." + sourceName + ")" + comma);
                    }
                    else
                    {
                        sb.AppendLine(indent + prop.Name + " = " + sourcePrefix + "." + sourceName + comma);
                    }
                }
            }
        }

        private static void AppendCollectionInitializer(
            StringBuilder sb,
            PropertyModel prop,
            string sourcePrefix,
            string indent,
            string comma)
        {
            var cSourceName = prop.SourcePropertyName;
            var cToMethod = prop.IsArray ? ".ToArray()" : ".ToList()";
            string cExpr;

            if (prop.NestedProperties.Length > 0)
            {
                var selectBody = new StringBuilder();
                selectBody.AppendLine("new " + prop.NestedDtoTypeFullName);
                selectBody.AppendLine(indent + "    {");

                var innerSb = new StringBuilder();
                AppendInitializer(innerSb, prop.NestedProperties, "x", indent + "        ");
                selectBody.Append(innerSb);

                selectBody.Append(indent + "    }");
                cExpr = sourcePrefix + "." + cSourceName + ".Select(x => " + selectBody + ")" + cToMethod;
            }
            else
            {
                cExpr = sourcePrefix + "." + cSourceName + cToMethod;
            }

            if (prop.IsNullable)
            {
                sb.AppendLine(indent + prop.Name + " = " + sourcePrefix + "." + cSourceName + " != null ? " + cExpr + " : null" + comma);
            }
            else
            {
                sb.AppendLine(indent + prop.Name + " = " + cExpr + comma);
            }
        }

        private static void AppendNestedInitializer(
            StringBuilder sb,
            PropertyModel prop,
            string sourcePrefix,
            string indent,
            string comma)
        {
            var nestedSource = sourcePrefix + "." + prop.Name;

            if (prop.IsNullable)
            {
                sb.AppendLine(indent + prop.Name + " = " + nestedSource + " != null ? new " + prop.NestedDtoTypeFullName);
                sb.AppendLine(indent + "{");

                AppendInitializer(sb, prop.NestedProperties, nestedSource, indent + "    ");

                sb.AppendLine(indent + "} : null" + comma);
            }
            else
            {
                sb.AppendLine(indent + prop.Name + " = new " + prop.NestedDtoTypeFullName);
                sb.AppendLine(indent + "{");

                AppendInitializer(sb, prop.NestedProperties, nestedSource, indent + "    ");

                sb.AppendLine(indent + "}" + comma);
            }
        }

        internal static string BuildFlattenedExpression(string sourcePrefix, PropertyModel prop)
        {
            if (prop.FlattenedPathHasNullableSegment)
            {
                // Build null-conditional chain: source.Address != null ? source.Address.City : default
                var segments = prop.FlattenedPath.Split('.');
                var nullCheckPath = sourcePrefix;
                var nullChecks = new StringBuilder();
                for (int s = 0; s < segments.Length - 1; s++)
                {
                    nullCheckPath += "." + segments[s];
                    if (nullChecks.Length > 0) nullChecks.Append(" && ");
                    nullChecks.Append(nullCheckPath + " != null");
                }
                var fullPath = sourcePrefix + "." + prop.FlattenedPath;
                return nullChecks + " ? " + fullPath + " : default";
            }
            else
            {
                return sourcePrefix + "." + prop.FlattenedPath;
            }
        }
    }
}
