using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Helpers;

namespace NUnit.Analyzers.ClassicModelAssertUsage
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public sealed class AreNotEqualClassicModelAssertUsageCodeFix
        : ClassicModelAssertUsageCodeFix
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(AnalyzerIdentifiers.AreNotEqualUsage);

        protected override (ArgumentSyntax ActualArgument, ArgumentSyntax? ConstraintArgument) ConstructActualAndConstraintArguments(
            Diagnostic diagnostic,
            IReadOnlyDictionary<string, ArgumentSyntax> argumentNamesToArguments)
        {
            var expectedArgument = argumentNamesToArguments[NUnitFrameworkConstants.NameOfExpectedParameter].WithNameColon(null);
            ExpressionSyntax constraint;

            if (CodeFixHelper.IsEmpty(expectedArgument.Expression))
            {
                constraint = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIs),
                        SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIsNot)),
                    SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIsEmpty));
            }
            else
            {
                constraint = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIs),
                            SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIsNot)),
                        SyntaxFactory.IdentifierName(NUnitFrameworkConstants.NameOfIsEqualTo)))
                    .WithArgumentList(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(expectedArgument)));
            }

            var actualArgument = argumentNamesToArguments[NUnitFrameworkConstants.NameOfActualParameter].WithNameColon(null);
            return (actualArgument, SyntaxFactory.Argument(constraint));
        }
    }
}
