using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class SyntaxTree
  {
    public readonly List<SyntaxNode> nodes;
    public SyntaxTree(List<SyntaxNode> nodes)
    {
      this.nodes = nodes;
    }

    public void WriteTree(IndentedTextWriter writer)
    {
      foreach(var node in nodes)
      {
        node.WriteTree(writer);
      }
    }
  }

  abstract class SyntaxNode
  {
    public SyntaxNode()
    {
      
    }

    public abstract void WriteTree(IndentedTextWriter writer);
  }

  class FunctionSyntax: SyntaxNode
  {
    public readonly Token identifierToken;
    public readonly TypeSyntax typeSyntax;
    public readonly List<StatementSyntax> statementSyntaxes;
    public FunctionSyntax(Token identifierToken, TypeSyntax typeSyntax, List<StatementSyntax> statementSyntaxes)
    {
      this.identifierToken = identifierToken;
      this.typeSyntax = typeSyntax;
      this.statementSyntaxes = statementSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"Function '{identifierToken.value}'");
      writer.Indent += 1;
      
      writer.WriteLine("type:");
      writer.Indent += 1;
      if (typeSyntax != null)
        typeSyntax.WriteTree(writer);
      else
        writer.WriteLine("null");
      writer.Indent -= 1;

      writer.WriteLine("statements:");
      for(var i=0; i<statementSyntaxes.Count; i++)
      {
        writer.Indent += 1;
        writer.WriteLine($"[{i}]: ");
        writer.Indent += 1;
        statementSyntaxes[i].WriteTree(writer);
        writer.Indent -= 1;
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class VariableOrCallSyntax: SyntaxNode
  {
    public readonly Token identifierToken;
    public readonly List<ExpressionSyntax> argumentExpressionSyntaxes;
    public VariableOrCallSyntax(Token identifierToken, List<ExpressionSyntax> argumentExpressionSyntaxes)
    {
      this.identifierToken = identifierToken;
      this.argumentExpressionSyntaxes = argumentExpressionSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"VariableOrCall '{identifierToken.value}'");
      writer.Indent += 1;

      for(var i=0; i<argumentExpressionSyntaxes.Count; i++)
      {
        writer.WriteLine($"[{i}]: ");
        writer.Indent += 1;
        argumentExpressionSyntaxes[i].WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class MemberAccessExpressionSyntax: SyntaxNode
  {
    public readonly List<VariableOrCallSyntax> variableOrCallSyntaxes;
    public MemberAccessExpressionSyntax(List<VariableOrCallSyntax> variableOrCallSyntaxes)
    {
      this.variableOrCallSyntaxes = variableOrCallSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"MemberAccessExpression");
      writer.Indent += 1;

      for(var i=0; i<variableOrCallSyntaxes.Count; i++)
      {
        writer.WriteLine($"[{i}]: ");
        writer.Indent += 1;
        variableOrCallSyntaxes[i].WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class StatementSyntax: SyntaxNode
  {
    public readonly MemberAccessExpressionSyntax memberAccessExpressionSyntax;
    public StatementSyntax(MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
      this.memberAccessExpressionSyntax = memberAccessExpressionSyntax;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"Statement");
      writer.Indent += 1;

      memberAccessExpressionSyntax.WriteTree(writer);

      writer.Indent -= 1;
    }
  }

  class ExpressionSyntax: SyntaxNode
  {
    public readonly Token literal;
    public ExpressionSyntax(Token literal)
    {
      this.literal = literal;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"Expression '{literal.value}'");
    }
  }

  class TypeSyntax: SyntaxNode
  {
    public readonly Token typeToken;
    public TypeSyntax(Token typeToken)
    {
      this.typeToken = typeToken;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"Type '{typeToken.value}'");
    }
  }
}