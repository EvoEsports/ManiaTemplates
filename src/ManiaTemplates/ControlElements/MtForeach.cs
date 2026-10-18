using ManiaTemplates.Exceptions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ManiaTemplates.ControlElements;

public class MtForeach : MtLoop
{
    public override string Keyword => "foreach";

    public static MtForeach FromString(string foreachAttributeValue, MtDataContext context, int nodeId, int depth)
    {
        //Let the C# parser split the value into the loop variables and the source.
        var statement = SyntaxFactory.ParseStatement($"foreach ({foreachAttributeValue}) {{}}");
        if (statement.ContainsDiagnostics)
        {
            throw new ParsingForeachLoopFailedException($"Failed to parse foreach loop: {foreachAttributeValue}");
        }

        var foundVariables = statement switch
        {
            //foreach (int i in numbers)
            ForEachStatementSyntax foreachStatement =>
                [CreateVariable(foreachStatement.Type, foreachStatement.Identifier.Text)],

            //foreach ((int a, string b) in pairs)
            ForEachVariableStatementSyntax foreachVariableStatement =>
                GetDeconstructedVariables(foreachVariableStatement.Variable).ToList(),

            _ => throw new ParsingForeachLoopFailedException($"Failed to parse foreach loop: {foreachAttributeValue}")
        };

        //Prevent user defining __index variable
        if (UsesReservedVariableName(foundVariables))
        {
            throw new ParsingForeachLoopFailedException("User defined variables must not start with __.");
        }

        return new MtForeach
        {
            Condition = foreachAttributeValue,
            Variables = foundVariables,
            Depth = depth,
            Context = CreateContext($"ForEachLoop{nodeId}", depth, foundVariables, context)
        };
    }

    /// <summary>
    /// Returns the variables declared by deconstructing the elements of the loop, including nested tuples.
    /// </summary>
    private static IEnumerable<MtLoopVariable> GetDeconstructedVariables(ExpressionSyntax expression)
    {
        return expression switch
        {
            TupleExpressionSyntax tuple =>
                tuple.Arguments.SelectMany(argument => GetDeconstructedVariables(argument.Expression)),

            DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax designation } declaration =>
                [CreateVariable(declaration.Type, designation.Identifier.Text)],

            //Discards (int _) and existing variables (a, _) do not declare new variables
            DeclarationExpressionSyntax { Designation: DiscardDesignationSyntax } or IdentifierNameSyntax => [],

            //var (a, b) has no explicit types
            DeclarationExpressionSyntax { Type.IsVar: true } => throw new ParsingForeachLoopFailedException(
                "You may not use var in foreach loops, please specify type."),

            _ => throw new ParsingForeachLoopFailedException(
                $"Failed to parse variables of foreach loop: {expression}")
        };
    }

    private static MtLoopVariable CreateVariable(TypeSyntax type, string name)
    {
        if (type.IsVar)
        {
            throw new ParsingForeachLoopFailedException("You may not use var in foreach loops, please specify type.");
        }

        return new MtLoopVariable
        {
            //Loop variables are passed to slots by value, so ref modifiers are dropped
            Type = (type is RefTypeSyntax refType ? refType.Type : type).ToString(),
            Name = name
        };
    }
}
