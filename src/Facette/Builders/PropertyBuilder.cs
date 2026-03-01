using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class PropertyBuilder
    {
        public static string Build(ImmutableArray<PropertyModel> properties, NullableMode nullableMode = NullableMode.Auto)
        {
            var sb = new StringBuilder();

            foreach (var prop in properties)
            {
                // Skip Custom-mapped, Flattened, and Inherited properties
                if (prop.MappingKind == MappingKind.Custom || prop.MappingKind == MappingKind.Flattened || prop.IsInherited)
                {
                    continue;
                }

                // Emit copied attributes
                foreach (var attr in prop.CopiedAttributes)
                {
                    sb.AppendLine("    " + attr);
                }

                var typeName = prop.TypeFullName;
                string defaultValue;

                if (nullableMode == NullableMode.AllNullable)
                {
                    if (prop.IsValueType && !prop.IsNullable)
                    {
                        typeName += "?";
                    }
                    else if (!prop.IsValueType && !typeName.EndsWith("?"))
                    {
                        typeName += "?";
                    }
                    defaultValue = "";
                }
                else if (nullableMode == NullableMode.AllRequired)
                {
                    // Strip trailing ? from reference types
                    if (!prop.IsValueType && typeName.EndsWith("?"))
                    {
                        typeName = typeName.Substring(0, typeName.Length - 1);
                    }
                    defaultValue = prop.IsValueType ? "" : " = default!;";
                }
                else
                {
                    // Auto mode — existing behavior
                    defaultValue = prop.IsValueType || prop.IsNullable ? "" : " = default!;";
                }

                sb.AppendLine("    public " + typeName + " " + prop.Name + " { get; init; }" + defaultValue);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
