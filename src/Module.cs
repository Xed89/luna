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
    public readonly String name;
    public readonly Type returnType;
    public readonly List<Statement> statements;
    public Function(Type type, String name, Type returnType, List<Statement> statements)
    {
      this.type = type;
      this.name = name;
      this.returnType = returnType;
      this.statements = statements;
    }

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

  class Statement
  {
    public readonly MemberAccess memberAccess;
    public Statement(MemberAccess memberAccess)
    {
      this.memberAccess = memberAccess;
    }
  }

  class MemberAccess
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

    public readonly List<VariableOrCall> variableOrCalls;
    public MemberAccess(List<VariableOrCall> variableOrCall)
    {
      this.variableOrCalls = variableOrCall;
    }
  }

  class VariableOrCall
  {
    public readonly ISymbol symbolToAccessOrCall;
    public readonly List<Expression> argumentExpressions;
    public VariableOrCall(ISymbol symbolToAccessOrCall, List<Expression> argumentExpressions)
    {
      this.symbolToAccessOrCall = symbolToAccessOrCall;
      this.argumentExpressions = argumentExpressions;
    }
  }

  class Expression
  {
    public readonly Token literal;
    public Expression(Token literal)
    {
      this.literal = literal;
    }
  }
}