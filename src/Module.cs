using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace LunaCompiler
{
  class Module
  {
    public readonly string name;
    public readonly IReadOnlyList<Type> types;
    public readonly Dictionary<String, String> stringConstants;
    public Module(string name,
                  IReadOnlyList<Type> types,
                  Dictionary<String, String> stringConstants)
    {
      this.name = name;
      this.types = types;
      this.stringConstants = stringConstants;
    }

    public Function FindEntryPoint()
    {
      foreach (var type in types)
      {
        var funMain = type.GetFunctionByName("main");
        if (funMain != null)
          return funMain;
      }
      return null;
    }

    public void Write(IndentedTextWriter writer)
    {
      writer.WriteLine($"Module {name}");
      writer.Indent += 1;
      foreach (var type in types)
      {
        type.Write(writer);
      }

      writer.WriteLine($"String constants");
      writer.Indent += 1;
      foreach (var kvp in stringConstants)
      {
        writer.WriteLine($"{kvp.Key} = \"{kvp.Value}\"");
      }
      writer.Indent -= 1;

      writer.Indent -= 1;
    }
  }

  interface ISymbol
  {
    String Name { get; }
    Type Type { get; }
  }

  class Type
  {
    public readonly String name;
    public readonly List<Function> functions;
    public Type(String name)
    {
      this.name = name;
      this.functions = new List<Function>();
    }

    public void AddFunction(Function f)
    {
      functions.Add(f);
    }

    public void Write(IndentedTextWriter writer)
    {
      writer.WriteLine($"Type {name}");
      writer.Indent += 1;
      foreach (var function in functions)
      {
        function.Write(writer);
      }
      writer.Indent -= 1;
    }

    public Function GetFunctionByName(string functionName)
    {
      foreach (var fun in functions)
      {
        if (fun.name == functionName)
          return fun;
      }
      return null;
    }
  }

  class Function : ISymbol
  {
    public readonly Type type;
    public readonly bool isStatic;
    public readonly String name;
    public readonly Type returnType;
    public readonly List<Statement> statements;
    public Function(Type type, bool isStatic, String name, Type returnType, List<Statement> statements)
    {
      this.type = type;
      this.isStatic = isStatic;
      this.name = name;
      this.returnType = returnType;
      this.statements = statements;
    }

    public String Name => name;
    public Type Type => type;

    public void Write(IndentedTextWriter writer)
    {
      if (returnType != null)
      {
        writer.WriteLine($"Function {name}(): {returnType.name}");
      }
      else
      {
        writer.WriteLine($"Function {name}()");
      }
      writer.Indent += 1;

      for (int i = 0; i < statements.Count; i++)
        writer.WriteLine(statements[i]);

      writer.Indent -= 1;
    }
  }

  abstract class Statement
  {
  }

  class VarOrCallChainMaybeAssignStatement: Statement
  {
    public readonly VarOrCallChain varOrCallChain;
    public readonly IExpression valueToAssignExpression;
    public VarOrCallChainMaybeAssignStatement(VarOrCallChain varOrCallChain, IExpression valueToAssignExpression)
    {
      this.varOrCallChain = varOrCallChain;
      this.valueToAssignExpression = valueToAssignExpression;
    }
  }

  class DeclarationStatement: Statement, ISymbol
  {
    public readonly bool isVar;
    public readonly Type type;
    public readonly String name;
    public readonly IExpression initializer;
    public DeclarationStatement(bool isVar, Type type, String name, IExpression initializer)
    {
      this.isVar = isVar;
      this.type = type;
      this.name = name;
      this.initializer = initializer;
    }

    public String Name => name;
    public Type Type => type;
  }

  class VarOrCallChain
  {
    /*
    Grammar:
    memberAccessExpression := variableOrCall {'.' variableOrCall}
    variableOrCall := identifier ['(' [expression {, expression}] ')']

    Examples:
    localVariable
    localVariable.toString()
    simpleFunction(9)
    Console.writeLine("Hello World!")
    min(max(5, 7), 9).toString()
    min(max(5, 7), 9).toString().length
    */

    public readonly List<VarOrCall> varOrCalls;
    public VarOrCallChain(List<VarOrCall> varOrCall)
    {
      this.varOrCalls = varOrCall;
    }
  }

  class VarOrCall
  {
    public readonly ISymbol symbolToAccessOrCall;
    public readonly List<IExpression> argumentExpressions;
    public VarOrCall(ISymbol symbolToAccessOrCall, List<IExpression> argumentExpressions)
    {
      this.symbolToAccessOrCall = symbolToAccessOrCall;
      this.argumentExpressions = argumentExpressions;
    }
  }

  interface IExpression
  {
    Type Type { get; }
  }

  class ExpressionBinOp: IExpression
  {
    public readonly Token op;
    public readonly IExpression leftExpr;
    public readonly IExpression rightExpr;
    public ExpressionBinOp(Token op, IExpression leftExpr, IExpression rightExpr)
    {
      this.op = op;
      this.leftExpr = leftExpr;
      this.rightExpr = rightExpr;
    }

    // TODO Consider type conversions
    public Type Type => leftExpr.Type;
  }

  class ExpressionParenthesized : IExpression
  {
    public readonly IExpression expression;
    public ExpressionParenthesized(IExpression expression)
    {
      this.expression = expression;
    }

    public Type Type => expression.Type;
  }

  class ExpressionLiteral: IExpression
  {
    public readonly Token literal;
    public readonly Type type;
    public ExpressionLiteral(Token literal, Type type)
    {
      this.literal = literal;
      this.type = type;
    }

    public Type Type => type;
  }
}