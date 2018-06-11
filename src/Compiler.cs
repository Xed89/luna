using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class Compiler
  {
    public readonly SyntaxTree syntaxTree;
    public Compiler(SyntaxTree syntaxTree)
    {
      this.syntaxTree = syntaxTree;
    }

    private List<ExternReference> externRefs;
    private Dictionary<String, String> stringConstants;

    public Module Compile()
    {
      var types = new List<Type>();
      externRefs = new List<ExternReference>();
      stringConstants = new Dictionary<string, string>();

      foreach (var node in syntaxTree.nodes)
      {
        if (node.GetType() == typeof(TypeDeclarationSyntax))
        {
          types.Add(CompileType((TypeDeclarationSyntax)node));
        }
        else
        {
          throw new ArgumentException($"Unexpected syntax {node.GetType().Name}");
        }
      }

      return new Module(syntaxTree.moduleName, types, stringConstants);
    }

    private ExternReference GetOrAddExternReference(String name)
    {
      int maxAddress = -1;
      for(int i=0; i<externRefs.Count; i++)
      {
        if (externRefs[i].Name == name)
        {
          return externRefs[i];
        }
        maxAddress = Math.Max(externRefs[i].IdxInSharedData, maxAddress);
      }
      var externRef = new ExternReference(name, maxAddress + 1);
      externRefs.Add(externRef);
      return externRef;
    }

    private Type CompileType(TypeDeclarationSyntax typeDeclaration)
    {
      var type = new Type(typeDeclaration.nameToken.value);

      foreach (var functionSyntax in typeDeclaration.functions)
      {
        var fun = CompileFunction(type, functionSyntax);
        type.AddFunction(fun);
      }

      return type;
    }

    private Function CompileFunction(Type type, FunctionSyntax functionSyntax)
    {
      Type returnType = null;
      if (functionSyntax.typeSyntax != null)
      {
        returnType = ResolveType(functionSyntax.typeSyntax);
      }

      var statements = new List<Statement>();
      foreach (var statementSyntax in functionSyntax.statementSyntaxes)
      {
        CompileFunctionStatement(statementSyntax, statements);
      }

      return new Function(type,
                          functionSyntax.nameToken.value,
                          returnType,
                          statements);
    }

    enum StatementCompileState
    {
      Start,
      TypeForStaticAccess,
      CallDone
    }

    private void CompileFunctionStatement(StatementSyntax statementSyntax, List<Statement> statements)
    {
      var variableOrCallSyntaxes = statementSyntax.memberAccessExpressionSyntax.variableOrCallSyntaxes;
      var state = StatementCompileState.Start;
      Type typeForStaticAccess = null;
      var variableOrCalls = new List<VariableOrCall>();
      foreach (var variableOrCallSyntax in variableOrCallSyntaxes)
      {
        switch (state)
        {
          case StatementCompileState.Start:
            {
              // The first can either be a variable, a function or a type used to call a static method or to access a static variable
              // TODO local variables, function args, instance args or function either static or not
              typeForStaticAccess = TryResolveTypeByName(variableOrCallSyntax.identifierToken);
              if (typeForStaticAccess != null)
              {
                // TODO Test this case
                if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
                  throw new ArgumentException($"{typeForStaticAccess.name} is a type and can't be invoked as a function");

                // Ok go on, the next will be a static method or field
                state = StatementCompileState.TypeForStaticAccess;
              }
              else
              {
                throw new ArgumentException($"Could not resolve {variableOrCallSyntax.identifierToken.value}");
              }
            }
            break;

          case StatementCompileState.TypeForStaticAccess:
            {
              if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
              {
                // It's a call, find the method
                var functionName = variableOrCallSyntax.identifierToken.value;
                var fun = typeForStaticAccess.GetFunctionByName(functionName);
                if (fun == null)
                {
                  throw new ArgumentException($"Type {typeForStaticAccess.name} has no function named '{functionName}'");
                }

                // Evaluate the args
                var argExprs = new List<Expression>();
                foreach(var arg in variableOrCallSyntax.argumentExpressionSyntaxes)
                {
                  if (arg.literal.type != TokenType.String)
                    throw new ArgumentException($"Argument type not supported");

                  argExprs.Add(new Expression(arg.literal));
                }
                variableOrCalls.Add(new VariableOrCall(fun, argExprs));

                state = StatementCompileState.CallDone;
              }
              else
              {
                throw new ArgumentException($"TODO Static field access not implemented");
              }
            }
            break;

          default:
            throw new ArgumentException($"Invalid statement compiler state: {state}");
        }
      }

      // Check final state
      switch (state)
      {
        // TODO Test the following error cases
        case StatementCompileState.Start:
          throw new ArgumentException($"Empty statement");

        case StatementCompileState.TypeForStaticAccess:
          throw new ArgumentException($"Member access expected");

        case StatementCompileState.CallDone:
          // The statemend ended with a call, it's valid.
          statements.Add(new Statement(new MemberAccess(variableOrCalls)));
          break;

        default:
          throw new ArgumentException($"Invalid statement compiler state: {state}");
      }
    }

    private void PrintInstruction(short instr)
    {
      Console.Write($"\\x{instr&0xff:x}\\x{(instr>>8)&0xff:x}");
    }

    private Type TryResolveTypeByName(Token nameToken)
    {
      // Try with built-in types
      switch (nameToken.value)
      {
        case "int": return new Type(nameToken.value);
        case "Console":
          {
            var type = new Type(nameToken.value);
            type.AddFunction(new Function(type, "writeLine", null, null));
            return type;
          }
      }

      return null;
    }

    private Type ResolveType(TypeSyntax typeSyntax)
    {
      var type = TryResolveTypeByName(typeSyntax.typeToken);
      if (type != null)
        return type;

      throw new ArgumentException($"Could not resolve type {typeSyntax.typeToken.value}");
    }
  }

  class ExternReference
  {
    public readonly string Name;
    public readonly int IdxInSharedData;
    public ExternReference(string name, int idxInSharedData)
    {
      Name = name;
      IdxInSharedData = idxInSharedData;
    }
  }
}