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
                var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                var comma = i < properties.Length - 1 ? "," : "";
                sb.AppendLine("            " + prop.Name + " = source." + sourceName + comma);
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
                var targetName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                var comma = i < properties.Length - 1 ? "," : "";
                sb.AppendLine("            " + targetName + " = this." + prop.Name + comma);
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }
    }
}
