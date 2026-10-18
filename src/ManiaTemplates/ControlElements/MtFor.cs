using ManiaTemplates.Exceptions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ManiaTemplates.ControlElements;

public class MtFor : MtLoop
{
    public override string Keyword => "for";

    public static MtFor FromString(string forAttributeValue, MtDataContext context, int nodeId, int depth)
    {
        //Let the C# parser split the value into initializer, condition and iterator.
        var statement = SyntaxFactory.ParseStatement($"for ({forAttributeValue}) {{}}");
        if (statement is not ForStatementSyntax forStatement || statement.ContainsDiagnostics)
        {
            throw new ParsingForLoopFailedException($"Failed to parse for loop: {forAttributeValue}");
        }

        var foundVariables = new List<MtLoopVariable>();
        var declaration = forStatement.Declaration;
        if (declaration != null)
        {
            if (declaration.Type.IsVar)
            {
                throw new ParsingForLoopFailedException("You may not use var in for loops, please specify type.");
            }

            foundVariables.AddRange(declaration.Variables.Select(variable => new MtLoopVariable
            {
                Type = declaration.Type.ToString(),
                Name = variable.Identifier.Text
            }));
        }

        //Prevent user defining __index variable
        if (UsesReservedVariableName(foundVariables))
        {
            throw new ParsingForLoopFailedException("User defined variables must not start with __.");
        }

        return new MtFor
        {
            Condition = forAttributeValue,
            Variables = foundVariables,
            Depth = depth,
            Context = CreateContext($"ForLoop{nodeId}", depth, foundVariables, context)
        };
    }
}
