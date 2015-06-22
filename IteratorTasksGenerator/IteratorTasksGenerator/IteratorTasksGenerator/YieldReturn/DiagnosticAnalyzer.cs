using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IteratorTasksGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class YieldReturnAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IteratorTasksGenerator";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.YieldReturnAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.YieldReturnAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.YieldReturnAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeYieldReturn, SyntaxKind.YieldReturnStatement);
        }

        private void AnalyzeYieldReturn(SyntaxNodeAnalysisContext context)
        {
            var yieldStatement = (YieldStatementSyntax)context.Node;
            if (!yieldStatement.IsYieldReturn()) { return; }

            var typeInfo = context.SemanticModel.GetTypeInfo(yieldStatement.Expression, context.CancellationToken);
            var type = typeInfo.Type as INamedTypeSymbol;
            if (IsIteratorTask(type))
            {
                var ifStatement = yieldStatement.AncestorsAndSelf().OfType<IfStatementSyntax>()
                    .FirstOrDefault(x => x.GetDeclaratedMember() == yieldStatement.GetDeclaratedMember());
                if (ifStatement != null)
                {
                    var memberAccsess = ifStatement.Condition.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
                    var ifConditionExp = memberAccsess?.Expression.WithoutTrivia().GetText().ToString();
                    var yieldExp = yieldStatement.Expression.WithoutTrivia().GetText().ToString();
                    if (ifConditionExp == yieldExp && memberAccsess.Name.Identifier.Text == "IsCompleted")
                    {
                        return;
                    }
                }

                var diagnostic = Diagnostic.Create(Rule, yieldStatement.GetLocation(), yieldStatement.Expression.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsIteratorTask(INamedTypeSymbol type)
        {
            return type.ContainingNamespace.Name == "IteratorTasks" && type.Name == "Task";
        }
    }
}
