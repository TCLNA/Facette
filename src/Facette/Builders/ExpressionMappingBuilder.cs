using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class ExpressionMappingBuilder
    {
        public static string Build(
            string dtoTypeName,
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties,
            bool hasBaseFacette = false)
        {
            var sb = new StringBuilder();
            var newKeyword = hasBaseFacette ? "new " : "";

            // MapExpression<TResult> method
            sb.AppendLine("    public " + newKeyword + "static System.Linq.Expressions.Expression<System.Func<" + sourceTypeFullName + ", TResult>> MapExpression<TResult>(");
            sb.AppendLine("        System.Linq.Expressions.Expression<System.Func<" + dtoTypeName + ", TResult>> expression)");
            sb.AppendLine("    {");
            sb.AppendLine("        var visitor = new _ExpressionVisitor();");
            sb.AppendLine("        var body = visitor.Visit(expression.Body);");
            sb.AppendLine("        return System.Linq.Expressions.Expression.Lambda<System.Func<" + sourceTypeFullName + ", TResult>>(");
            sb.AppendLine("            body, visitor.SourceParam);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Nested ExpressionVisitor class
            sb.AppendLine("    private sealed class _ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor");
            sb.AppendLine("    {");
            sb.AppendLine("        public readonly System.Linq.Expressions.ParameterExpression SourceParam =");
            sb.AppendLine("            System.Linq.Expressions.Expression.Parameter(typeof(" + sourceTypeFullName + "), \"src\");");
            sb.AppendLine("        private System.Linq.Expressions.ParameterExpression? _dtoParam;");
            sb.AppendLine();
            sb.AppendLine("        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (node.Type == typeof(" + dtoTypeName + "))");
            sb.AppendLine("            {");
            sb.AppendLine("                _dtoParam = node;");
            sb.AppendLine("                return SourceParam;");
            sb.AppendLine("            }");
            sb.AppendLine("            return base.VisitParameter(node);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override System.Linq.Expressions.Expression VisitMember(System.Linq.Expressions.MemberExpression node)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (node.Expression is System.Linq.Expressions.ParameterExpression param");
            sb.AppendLine("                && param.Type == typeof(" + dtoTypeName + "))");
            sb.AppendLine("            {");
            sb.AppendLine("                var memberName = node.Member.Name;");

            // Build mapping dictionary
            bool first = true;
            foreach (var prop in properties)
            {
                var condition = first ? "if" : "else if";
                first = false;

                sb.AppendLine("                " + condition + " (memberName == \"" + prop.Name + "\")");
                sb.AppendLine("                {");

                var sourceExpr = BuildSourceExpression(prop, sourceTypeFullName);
                sb.AppendLine("                    " + sourceExpr);

                sb.AppendLine("                }");
            }

            sb.AppendLine("                return base.VisitMember(node);");
            sb.AppendLine("            }");
            sb.AppendLine("            return base.VisitMember(node);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }

        private static string BuildSourceExpression(PropertyModel prop, string sourceTypeFullName)
        {
            if (prop.MappingKind == MappingKind.Flattened)
            {
                // Build nested member access: src.Address.City
                return "return " + BuildMemberAccessChain("SourceParam", sourceTypeFullName, prop.FlattenedPath) + ";";
            }

            var sourceName = prop.MappingKind == MappingKind.Custom
                ? prop.SourcePropertyName : prop.Name;

            if (prop.EnumConversion == EnumConversionKind.EnumToString)
            {
                // src.Status.ToString() — use method call expression
                return "return System.Linq.Expressions.Expression.Call("
                    + "System.Linq.Expressions.Expression.Property(SourceParam, \"" + sourceName + "\"), "
                    + "\"ToString\", System.Type.EmptyTypes);";
            }

            if (prop.EnumConversion == EnumConversionKind.EnumToInt
                || prop.EnumConversion == EnumConversionKind.IntToEnum
                || prop.EnumConversion == EnumConversionKind.EnumToEnum)
            {
                return "return System.Linq.Expressions.Expression.Convert("
                    + "System.Linq.Expressions.Expression.Property(SourceParam, \"" + sourceName + "\"), "
                    + "node.Type);";
            }

            if (!string.IsNullOrEmpty(prop.ConvertMethod))
            {
                // Call static method: ConvertContainingType.ConvertMethod(src.Prop)
                return "return System.Linq.Expressions.Expression.Call("
                    + "typeof(" + prop.ConvertContainingType + ").GetMethod(\"" + prop.ConvertMethod + "\")!, "
                    + "System.Linq.Expressions.Expression.Property(SourceParam, \"" + sourceName + "\"));";
            }

            // Direct or Custom: src.SourcePropertyName
            return "return System.Linq.Expressions.Expression.Property(SourceParam, \"" + sourceName + "\");";
        }

        private static string BuildMemberAccessChain(string paramExpr, string rootType, string path)
        {
            var segments = path.Split('.');
            var result = "System.Linq.Expressions.Expression.Property(" + paramExpr + ", \"" + segments[0] + "\")";

            for (int i = 1; i < segments.Length; i++)
            {
                result = "System.Linq.Expressions.Expression.Property(" + result + ", \"" + segments[i] + "\")";
            }

            return result;
        }
    }
}
