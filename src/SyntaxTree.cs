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
    public readonly Token nameToken;
    public readonly List<FunctionArgSyntax> argumentSyntaxes;
    public readonly TypeSyntax typeSyntax;
    public readonly List<StatementSyntax> statementSyntaxes;
    public FunctionSyntax(Token identifierToken, List<FunctionArgSyntax> argumentSyntaxes, TypeSyntax typeSyntax, List<StatementSyntax> statementSyntaxes)
    {
      this.nameToken = identifierToken;
      this.argumentSyntaxes = argumentSyntaxes;
      this.typeSyntax = typeSyntax;
      this.statementSyntaxes = statementSyntaxes;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"Function '{nameToken.value}'");
      writer.Indent += 1;

      if (argumentSyntaxes.Count > 0)
      {
        writer.WriteLine("arguments:");
        writer.Indent += 1;
        for (var i = 0; i < argumentSyntaxes.Count; i++)
        {
          writer.Indent += 1;
          writer.WriteLine($"[{i}]: ");
          writer.Indent += 1;
          argumentSyntaxes[i].WriteTree(writer);
          writer.Indent -= 1;
          writer.Indent -= 1;
        }
        writer.Indent -= 1;
      }

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

  class FunctionArgSyntax : SyntaxNode
  {
    public readonly Token nameToken;
    public readonly TypeSyntax typeSyntax;
    public FunctionArgSyntax(Token nameToken, TypeSyntax typeSyntax)
    {
      this.nameToken = nameToken;
      this.typeSyntax = typeSyntax;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"FunctionArg '{nameToken.value}'");
      writer.Indent += 1;

      writer.WriteLine("type:");
      writer.Indent += 1;
      if (typeSyntax != null)
        typeSyntax.WriteTree(writer);
      else
        writer.WriteLine("null");
      writer.Indent -= 1;

      writer.Indent -= 1;
    }
  }

  class VarOrCallSyntax : SyntaxNode
  {
    public readonly Token identifierToken;
    public readonly List<IExpressionSyntax> argumentExpressionSyntaxes;
    public VarOrCallSyntax(Token identifierToken, List<IExpressionSyntax> argumentExpressionSyntaxes)
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

  class VarOrCallChainSyntax : SyntaxNode, IExpressionSyntax
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
    public readonly IExpressionSyntax valueToAssignExpression;
    public VarOrCallChainMaybeAssignStatementSyntax(VarOrCallChainSyntax varOrCallChainSyntax, IExpressionSyntax valueToAssignExpression)
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
    public readonly IExpressionSyntax initializer;
    public DeclarationStatementSyntax(bool isVar, Token identifierToken, IExpressionSyntax initializer)
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

  class ReturnStatementSyntax : StatementSyntax
  {
    public readonly IExpressionSyntax value;
    public ReturnStatementSyntax(IExpressionSyntax value)
    {
      this.value = value;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine(GetType().Name);
      writer.Indent += 1;

      if (value != null)
      {
        writer.WriteLine($"value:");
        writer.Indent += 1;
        value.WriteTree(writer);
        writer.Indent -= 1;
      }

      writer.Indent -= 1;
    }
  }

  interface IExpressionSyntax
  {
    void WriteTree(IndentedTextWriter writer);
  }

  class ExpressionBinOpSyntax : SyntaxNode, IExpressionSyntax
  {
    public readonly Token op;
    public readonly IExpressionSyntax leftExpressionSyntax;
    public readonly IExpressionSyntax rightExpressionSyntax;
    public ExpressionBinOpSyntax(Token op,
                                 IExpressionSyntax leftExpressionSyntax,
                                 IExpressionSyntax rightExpressionSyntax)
    {
      this.op = op;
      this.leftExpressionSyntax = leftExpressionSyntax;
      this.rightExpressionSyntax = rightExpressionSyntax;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"{GetType().Name}");
      writer.Indent += 1;

      writer.WriteLine($"Op: '{op.value}'");

      writer.WriteLine($"left: ");
      writer.Indent += 1;
      leftExpressionSyntax.WriteTree(writer);
      writer.Indent -= 1;

      writer.WriteLine($"right: ");
      writer.Indent += 1;
      rightExpressionSyntax.WriteTree(writer);
      writer.Indent -= 1;

      writer.Indent -= 1;
    }
  }

  class ExpressionParenthesizedSyntax : SyntaxNode, IExpressionSyntax
  {
    public readonly IExpressionSyntax expression;
    public ExpressionParenthesizedSyntax(IExpressionSyntax expression)
    {
      this.expression = expression;
    }

    public override void WriteTree(IndentedTextWriter writer)
    {
      writer.WriteLine($"{GetType().Name}");
      writer.Indent += 1;
      expression.WriteTree(writer);
      writer.Indent -= 1;
    }
  }

  class ExpressionLiteralSyntax : SyntaxNode, IExpressionSyntax
  {
    public readonly Token literal;
    public ExpressionLiteralSyntax(Token literal)
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