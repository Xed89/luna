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
    public Module(string name,
                  IReadOnlyList<Type> types)
    {
      this.name = name;
      this.types = types;
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
    public readonly List<FunctionArg> arguments;
    public readonly Type returnType;
    public readonly List<Statement> statements;
    public Function(Type type, bool isStatic, String name, List<FunctionArg> arguments, Type returnType)
    {
      this.type = type;
      this.isStatic = isStatic;
      this.name = name;
      this.arguments = arguments;
      this.returnType = returnType;
      this.statements = new List<Statement>();
    }

    public String Name => name;
    public Type Type => returnType;

    public void Write(IndentedTextWriter writer)
    {
      writer.Write($"Function {name}(");
      var first = true;
      foreach(var arg in arguments)
      {
        if (!first)
          writer.Write($", ");
        first = false;
        
        writer.Write($"{arg.name}: {arg.type.name}");
      }
      writer.Write(")");
      if (returnType != null)
      {
        writer.Write($": {returnType.name}");
      }
      writer.WriteLine("");

      writer.Indent += 1;

      for (int i = 0; i < statements.Count; i++)
        writer.WriteLine(statements[i]);

      writer.Indent -= 1;
    }
  }

  class FunctionArg: ISymbol
  {
    public readonly String name;
    public readonly Type type;
    public FunctionArg(String name, Type type)
    {
      this.name = name;
      this.type = type;
    }

    public String Name => name;
    public Type Type => type;
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

  class VarOrCallChain: IExpression
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

    // The varOrCall chain has the same type of the last symbol accessed or called
    public Type Type => varOrCalls[varOrCalls.Count - 1].symbolToAccessOrCall.Type;
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

  class ReturnStatement : Statement
  {
    public readonly IExpression value;
    public ReturnStatement(IExpression value)
    {
      this.value = value;
    }
  }

  class IfStatement : Statement
  {
    public readonly IExpression condition;
    public readonly List<Statement> trueBranchStatements;
    public readonly List<Statement> falseBranchStatements;
    public IfStatement(IExpression condition, List<Statement> trueBranchStatements, List<Statement> falseBranchStatements)
    {
      this.condition = condition;
      this.trueBranchStatements = trueBranchStatements;
      this.falseBranchStatements = falseBranchStatements;
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
    public readonly Type type;
    public ExpressionBinOp(Token op, IExpression leftExpr, IExpression rightExpr, Type type)
    {
      this.op = op;
      this.leftExpr = leftExpr;
      this.rightExpr = rightExpr;
      this.type = type;
    }

    public Type Type => type;
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