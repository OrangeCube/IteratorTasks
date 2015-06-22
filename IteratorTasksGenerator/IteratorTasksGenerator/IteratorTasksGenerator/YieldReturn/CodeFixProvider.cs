using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace IteratorTasksGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(YieldReturnCodeFixProvider)), Shared]
    public class YieldReturnCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(YieldReturnAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);
            var yieldStatement = node as YieldStatementSyntax;

            if (yieldStatement == null) { return; }

            context.RegisterCodeFix(
                CodeAction.Create("Make statements", c => MakeTaskYieldReturnStatements(context.Document, yieldStatement, c)),
                diagnostic);
        }

        private async Task<Document> MakeTaskYieldReturnStatements(Document document, YieldStatementSyntax yieldStatement, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(ct);

            var nodes = yieldStatement.CreateFixedYieldReturn(semanticModel);
            root = root.ReplaceNode(yieldStatement, nodes);
            return document.WithSyntaxRoot(root);
        }

    }
}