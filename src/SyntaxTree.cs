using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class SyntaxTree
  {
    public readonly String moduleName;
    public readonly List<SyntaxNode> nodes;
    public SyntaxTree(String moduleName, List<SyntaxNode> nodes)
    {
      this.moduleName = moduleName;
      this.nodes = nodes;
    }

    public void WriteTree(IndentedTextWriter writer)
    {
      foreach (var node in nodes)
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

  class TypeDeclarationSyntax : SyntaxNode
  {
    public readonly Token nameToken;
    public readonly IReadOnlyList<FunctionSyntax> functions;
    public TypeDeclarationSyntax(Token nameToken, IReadOnlyList<FunctionSyntax> functions)
    {
      this.nameToken = nameToken;
      this.functions = functions;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"TypeDeclaration '{nameToken.value}'");
      writer.Indent += 1;

      for (var i = 0; i < functions.Count; i++)
      {
        writer.WriteLine($"[{i}]: ");
        writer.Indent += 1;
        functions[i].WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class FunctionSyntax : SyntaxNode
  {
    public readonly bool isStatic;
    public readonly Token nameToken;
    public readonly TypeSyntax typeSyntax;
    public readonly List<StatementSyntax> statementSyntaxes;
    public FunctionSyntax(bool isStatic, Token identifierToken, TypeSyntax typeSyntax, List<StatementSyntax> statementSyntaxes)
    {
      this.isStatic = isStatic;
      this.nameToken = identifierToken;
      this.typeSyntax = typeSyntax;
      this.statementSyntaxes = statementSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      var staticKeyword = isStatic ? " (static)": "";
      writer.WriteLine($"Function '{nameToken.value}'{staticKeyword}");
      writer.Indent += 1;

      writer.WriteLine("type:");
      writer.Indent += 1;
      if (typeSyntax != null)
        typeSyntax.WriteTree(writer);
      else
        writer.WriteLine("null");
      writer.Indent -= 1;

      writer.WriteLine("statements:");
      for (var i = 0; i < statementSyntaxes.Count; i++)
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

  class VarOrCallSyntax : SyntaxNode
  {
    public readonly Token identifierToken;
    public readonly List<ExpressionSyntax> argumentExpressionSyntaxes;
    public VarOrCallSyntax(Token identifierToken, List<ExpressionSyntax> argumentExpressionSyntaxes)
    {
      this.identifierToken = identifierToken;
      this.argumentExpressionSyntaxes = argumentExpressionSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"VariableOrCall '{identifierToken.value}'");
      writer.Indent += 1;

      // If it's not null we have a call
      if (argumentExpressionSyntaxes != null)
      {
        for (var i = 0; i < argumentExpressionSyntaxes.Count; i++)
        {
          writer.WriteLine($"[{i}]: ");
          writer.Indent += 1;
          argumentExpressionSyntaxes[i].WriteTree(writer);
          writer.Indent -= 1;
        }
      }

      writer.Indent -= 1;
    }
  }

  class VarOrCallChainSyntax : SyntaxNode
  {
    public readonly List<VarOrCallSyntax> variableOrCallSyntaxes;
    public VarOrCallChainSyntax(List<VarOrCallSyntax> variableOrCallSyntaxes)
    {
      this.variableOrCallSyntaxes = variableOrCallSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine(GetType().Name);
      writer.Indent += 1;

      for (var i = 0; i < variableOrCallSyntaxes.Count; i++)
      {
        writer.WriteLine($"[{i}]: ");
        writer.Indent += 1;
        variableOrCallSyntaxes[i].WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  abstract class StatementSyntax : SyntaxNode
  {
  }

  class VarOrCallChainMaybeAssignStatementSyntax : StatementSyntax
  {
    public readonly VarOrCallChainSyntax varOrCallChainSyntax;
    public readonly ExpressionSyntax valueToAssignExpression;
    public VarOrCallChainMaybeAssignStatementSyntax(VarOrCallChainSyntax varOrCallChainSyntax, ExpressionSyntax valueToAssignExpression)
    {
      this.varOrCallChainSyntax = varOrCallChainSyntax;
      this.valueToAssignExpression = valueToAssignExpression;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine(GetType().Name);
      writer.Indent += 1;

      varOrCallChainSyntax.WriteTree(writer);
      if (valueToAssignExpression != null)
      {
        writer.Indent += 1;
        valueToAssignExpression.WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class DeclarationStatementSyntax : StatementSyntax
  {
    public readonly bool isVar;
    public readonly Token identifierToken;
    public readonly ExpressionSyntax initializer;
    public DeclarationStatementSyntax(bool isVar, Token identifierToken, ExpressionSyntax initializer)
    {
      this.isVar = isVar;
      this.identifierToken = identifierToken;
      this.initializer = initializer;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine(GetType().Name);
      writer.Indent += 1;

      var letOrVarStr = isVar ? "var" : "let";
      writer.WriteLine($"{letOrVarStr} {identifierToken.value}");
      if (initializer != null)
      {
        writer.Indent += 1;
        initializer.WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  class ExpressionSyntax : SyntaxNode
  {
    public readonly Token literal;
    public ExpressionSyntax(Token literal)
    {
      this.literal = literal;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"{GetType().Name} '{literal.value}'");
    }
  }

  class TypeSyntax : SyntaxNode
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