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
    public static class SyntaxExtensions
    {
        private static readonly SyntaxToken ReturnKeyword = SyntaxFactory.Token(SyntaxKind.ReturnKeyword);
        private static readonly SyntaxToken BreakKeyword = SyntaxFactory.Token(SyntaxKind.BreakKeyword);

        public static bool IsYieldReturn(this YieldStatementSyntax node)
        {
            return node.ReturnOrBreakKeyword.Text == "return";
        }

        public static bool IsYieldBreak(this YieldStatementSyntax node)
        {
            return node.ReturnOrBreakKeyword == BreakKeyword;
        }

        public static MemberDeclarationSyntax GetDeclaratedMember(this SyntaxNode node)
        {
            return node.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        }

        private const string TaskName = "t";

        public static IEnumerable<SyntaxNode> CreateFixedYieldReturn(this YieldStatementSyntax yieldStatement, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(yieldStatement.Expression);
            var type = typeInfo.Type as INamedTypeSymbol;

            var task = SyntaxFactory.IdentifierName(TaskName);
            yield return CreateLocalVariableDeclaration(TaskName, yieldStatement.Expression);
            yield return CreateYieldReturnIfNotCompleted(task);
            yield return CreateThrowIfException(task);

            if (type.TypeArguments.Count() == 1)
            {
                var exp = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, SyntaxFactory.IdentifierName("Result"));
                yield return CreateLocalVariableDeclaration("_result", exp);
            }
        }
        
        private static IdentifierNameSyntax Var = SyntaxFactory.IdentifierName("var");

        /// <summary>
        /// Create local variable declaration statement with initializer from ExpressionSyntax.
        /// </summary>
        private static SyntaxNode CreateLocalVariableDeclaration(string name, ExpressionSyntax exp)
        {
            return SyntaxFactory.LocalDeclarationStatement(
               SyntaxFactory.VariableDeclaration(Var).AddVariables(
                   SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(exp))
               )
           );
        }

        /// <summary>
        /// Create If statement that execute yield return if not completed the <see cref="Task"/> or <see cref="Task{T}"/> object.
        /// </summary>
        /// <remarks>
        /// if (!task.IsCompleted)
        ///     yield return task;
        /// </remarks>
        private static SyntaxNode CreateYieldReturnIfNotCompleted(IdentifierNameSyntax task)
        {
            return SyntaxFactory.IfStatement(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, SyntaxFactory.IdentifierName("IsCompleted")),
                SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, task)
            );
        }

        /// <summary>
        /// Create ExpressionSyntax node that invoke ThrowIfException().
        /// </summary>
        /// <remarks>
        /// task.ThrowIfException()
        /// </remarks>
        private static SyntaxNode CreateThrowIfException(IdentifierNameSyntax task)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, SyntaxFactory.IdentifierName("ThrowIfException"))
                )
            );
        }

    }
}
