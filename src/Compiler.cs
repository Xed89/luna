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
          throw new CompilerException($"Unexpected syntax {node.GetType().Name}");
        }
      }

      return new Module(syntaxTree.moduleName, types, stringConstants);
    }

    private ExternReference GetOrAddExternReference(String name)
    {
      int maxAddress = -1;
      for (int i = 0; i < externRefs.Count; i++)
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

    // Current scope data
    private List<DeclarationStatement> localVariables;
    private ISymbol FindIdentifier(Token identifier)
    {
      var result = FindIdentifierOrNull(identifier);
      if (result != null)
        return result;

      throw new CompilerException($"Could not find identifier {identifier.value}", identifier);
    }

    private ISymbol FindIdentifierOrNull(Token identifier)
    {
      // token is an identifier
      foreach(var lv in localVariables)
      {
        if (lv.name == identifier.value)
        {
          return lv;
        }
      }

      return null;
    }

    private Function CompileFunction(Type type, FunctionSyntax functionSyntax)
    {
      localVariables = new List<DeclarationStatement>();

      Type returnType = null;
      if (functionSyntax.typeSyntax != null)
      {
        returnType = ResolveType(functionSyntax.typeSyntax);
      }

      var statements = new List<Statement>();
      foreach (var statementSyntax in functionSyntax.statementSyntaxes)
      {
        if (statementSyntax.GetType() == typeof(VarOrCallChainMaybeAssignStatementSyntax))
        {
          CompileFunctionStatement_VarOrCallChainMaybeAssignStatement((VarOrCallChainMaybeAssignStatementSyntax)statementSyntax, statements);
        }
        else if (statementSyntax.GetType() == typeof(DeclarationStatementSyntax))
        {
          CompileFunctionStatement_DeclarationStatement((DeclarationStatementSyntax)statementSyntax, statements);
        }
        else
        {
          throw new CompilerException($"Unknown statement type: {statementSyntax.GetType().Name}");
        }
      }

      return new Function(type,
                          functionSyntax.isStatic,
                          functionSyntax.nameToken.value,
                          returnType,
                          statements);
    }

    enum StatementCompileState
    {
      Start,
      TypeForStaticAccess,
      CallDone,
      AccessDone
    }

    private void CompileFunctionStatement_VarOrCallChainMaybeAssignStatement(VarOrCallChainMaybeAssignStatementSyntax statementSyntax, List<Statement> statements)
    {
      var variableOrCallSyntaxes = statementSyntax.varOrCallChainSyntax.variableOrCallSyntaxes;
      var state = StatementCompileState.Start;
      Type typeForStaticAccess = null;
      var variableOrCalls = new List<VarOrCall>();
      foreach (var variableOrCallSyntax in variableOrCallSyntaxes)
      {
        switch (state)
        {
          case StatementCompileState.Start:
            {
              // The first can either be a variable, a function or a type used to call a static method or to access a static variable
              // TODO local variables, function args, instance args or function either static or not
              typeForStaticAccess = TryResolveTypeByTokenValue(variableOrCallSyntax.identifierToken);
              if (typeForStaticAccess != null)
              {
                // TODO Test this case
                if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
                  throw new CompilerException($"{typeForStaticAccess.name} is a type and can't be invoked as a function", variableOrCallSyntax.identifierToken);

                // Ok go on, the next will be a static method or field
                state = StatementCompileState.TypeForStaticAccess;
                break;
              }

              var identifier = FindIdentifierOrNull(variableOrCallSyntax.identifierToken);
              if (identifier != null)
              {
                // TODO Test this case
                if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
                  throw new CompilerException($"{identifier.Name} is a type and can't be invoked as a function", variableOrCallSyntax.identifierToken);

                variableOrCalls.Add(new VarOrCall(identifier, null));
                state = StatementCompileState.AccessDone;
                break;
              }
              
              throw new CompilerException($"Could not resolve {variableOrCallSyntax.identifierToken.value}", variableOrCallSyntax.identifierToken);
            }

          case StatementCompileState.TypeForStaticAccess:
            {
              if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
              {
                // It's a call, find the method
                var functionName = variableOrCallSyntax.identifierToken.value;
                var fun = typeForStaticAccess.GetFunctionByName(functionName);
                if (fun == null)
                {
                  throw new CompilerException($"Type {typeForStaticAccess.name} has no function named '{functionName}'", variableOrCallSyntax.identifierToken);
                }

                // Evaluate the args
                var argExprs = new List<IExpression>();
                foreach (var arg in variableOrCallSyntax.argumentExpressionSyntaxes)
                {
                  argExprs.Add(CompileExpression(arg));
                }
                variableOrCalls.Add(new VarOrCall(fun, argExprs));

                state = StatementCompileState.CallDone;
              }
              else
              {
                throw new CompilerException($"TODO Static field access not implemented");
              }
            }
            break;

          default:
            throw new CompilerException($"Invalid statement compiler state: {state}");
        }
      }

      // Check final state
      switch (state)
      {
        // TODO Test the following error cases
        case StatementCompileState.Start:
          throw new CompilerException($"Empty statement");

        case StatementCompileState.TypeForStaticAccess:
          throw new CompilerException($"Member access expected");

        case StatementCompileState.CallDone:
          // The statemend ended with a call, it's valid.
          statements.Add(new VarOrCallChainMaybeAssignStatement(new VarOrCallChain(variableOrCalls), null));
          break;

        case StatementCompileState.AccessDone:
          // The statement ended with a varible access, an assign is expected to form a valid statement
          if (statementSyntax.valueToAssignExpression == null)
          {
            throw new CompilerException($"Assign operator expected");
          }

          var assignedSymbol = (DeclarationStatement)(variableOrCalls[variableOrCalls.Count-1].symbolToAccessOrCall);
          if (!assignedSymbol.isVar)
          {
            throw new CompilerException($"Variable {assignedSymbol.name} can't be modified because declared with 'let'");
          }

          // The statemend ended with a call, it's valid.
          var valueToAssignExpression = CompileExpression(statementSyntax.valueToAssignExpression);
          // TODO Check the type assigned agains the variable type
          statements.Add(new VarOrCallChainMaybeAssignStatement(new VarOrCallChain(variableOrCalls), valueToAssignExpression));
          break;

        default:
          throw new CompilerException($"Invalid statement compiler final state: {state}");
      }
    }

    private void CompileFunctionStatement_DeclarationStatement(DeclarationStatementSyntax declarationStatement, List<Statement> statements)
    {
      IExpression initializer = null;
      Type type = null;
      if (declarationStatement.initializer != null)
      {
        initializer = CompileExpression(declarationStatement.initializer);
        type = initializer.Type;
      }
      else
      {
        throw new ArgumentException("Could not determine type for variable");
      }

      // TODO Check duplicate variables

      var ds = new DeclarationStatement(declarationStatement.isVar,
                                        type,
                                        declarationStatement.identifierToken.value,
                                        initializer);
      localVariables.Add(ds);
      statements.Add(ds);
    }

    private IExpression CompileExpression(IExpressionSyntax expressionSyntax, 
                                          int parentOpPrecedence = int.MaxValue)
    {
      if (expressionSyntax.GetType() == typeof (ExpressionBinOpSyntax))
      {
        var expressionBinOpSyntax = (ExpressionBinOpSyntax)expressionSyntax;
        return new ExpressionBinOp(expressionBinOpSyntax.op,
                                   CompileExpression(expressionBinOpSyntax.leftExpressionSyntax),
                                   CompileExpression(expressionBinOpSyntax.rightExpressionSyntax));
      }
      else if (expressionSyntax.GetType() == typeof (ExpressionParenthesizedSyntax))
      {
        var expressionParenthesizedSyntax = (ExpressionParenthesizedSyntax)expressionSyntax;
        return new ExpressionParenthesized(CompileExpression(expressionParenthesizedSyntax.expression));
      }
      else if (expressionSyntax.GetType() == typeof (ExpressionLiteralSyntax))
      {
        var expressionLiteralSyntax = (ExpressionLiteralSyntax)expressionSyntax;
        return CompileExpressionLiteral(expressionLiteralSyntax);
      }
      else
      {
        throw new ArgumentException($"Could not determine type of expression");
      }
    }

    private IExpression CompileExpressionLiteral(ExpressionLiteralSyntax expressionSyntax)
    {
      Type type = null;
      if (expressionSyntax.literal.type == TokenType.String)
      {
        type = TryResolveTypeByName("string");
      }
      else if (expressionSyntax.literal.type == TokenType.Number)
      {
        // TODO Decimal types
        type = TryResolveTypeByName("int");
      }
      else if (expressionSyntax.literal.type == TokenType.Identifier)
      {
        // Find the identifier, it can be a local variable, a function argument, a global, ecc..
        type = FindIdentifier(expressionSyntax.literal).Type;
      }

      if (type == null)
      {
        throw new ArgumentException($"Could not determine type of expression");
      }

      return new ExpressionLiteral(expressionSyntax.literal, type);
    }

    private void PrintInstruction(short instr)
    {
      Console.Write($"\\x{instr & 0xff:x}\\x{(instr >> 8) & 0xff:x}");
    }

    private Type TryResolveTypeByTokenValue(Token nameToken)
    {
      return TryResolveTypeByName(nameToken.value);
    }

    private Type TryResolveTypeByName(String name)
    {
      // Try with built-in types
      switch (name)
      {
        case "int": return new Type(name);
        case "string": return new Type(name);
        case "Console":
          {
            var type = new Type(name);
            type.AddFunction(new Function(type, true, "writeLine", null, null));
            return type;
          }
      }

      return null;
    }

    private Type ResolveType(TypeSyntax typeSyntax)
    {
      var type = TryResolveTypeByTokenValue(typeSyntax.typeToken);
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