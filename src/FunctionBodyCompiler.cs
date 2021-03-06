using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class FunctionBodyCompiler
  {
    private readonly Function function;
    private readonly FunctionSyntax functionSyntax;
    private readonly TypeResolver typeResolver;
    private readonly List<CompilerException> errors;
    private List<DeclarationStatement> localVariables;
    public FunctionBodyCompiler(Function function, FunctionSyntax functionSyntax, TypeResolver typeResolver, List<CompilerException> errors)
    {
      this.function = function;
      this.functionSyntax = functionSyntax;
      this.typeResolver = typeResolver;
      this.errors = errors;
      localVariables = new List<DeclarationStatement>();
    }

    public void Compile()
    {
      CompileStatements(functionSyntax.statementSyntaxes, function.statements);
    }

    private void CompileStatements(List<StatementSyntax> statementSyntaxes, List<Statement> statements)
    {
      foreach (var statementSyntax in statementSyntaxes)
      {
        try {
          if (statementSyntax.GetType() == typeof(VarOrCallChainMaybeAssignStatementSyntax))
          {
            CompileFunctionStatement_VarOrCallChainMaybeAssignStatement((VarOrCallChainMaybeAssignStatementSyntax)statementSyntax, statements);
          }
          else if (statementSyntax.GetType() == typeof(DeclarationStatementSyntax))
          {
            CompileFunctionStatement_DeclarationStatement((DeclarationStatementSyntax)statementSyntax, statements);
          }
          else if (statementSyntax.GetType() == typeof(ReturnStatementSyntax))
          {
            CompileFunctionStatement_ReturnStatement((ReturnStatementSyntax)statementSyntax, statements);
          }
          else if (statementSyntax.GetType() == typeof(IfStatementSyntax))
          {
            CompileFunctionStatement_IfStatement((IfStatementSyntax)statementSyntax, statements);
          }
          else
          {
            throw new CompilerException($"Unknown statement type: {statementSyntax.GetType().Name}");
          }
        } catch (CompilerException ex) {
          errors.Add(ex);
        }
      }
    }

    private ISymbol FindIdentifier(Token identifier)
    {
      var result = FindIdentifierOrNull(identifier);
      if (result != null)
        return result;

      throw new CompilerException($"Could not find identifier {identifier.value}", identifier);
    }

    private ISymbol FindIdentifierOrNull(Token identifier)
    {
      // Try local variables
      var x = FindLocalVariableOrNull(identifier.value);
      if (x != null) return x;

      // Try function arguments
      var y = FindFunctionArgumentOrNull(identifier.value);
      if (y != null) return y;

      // Try functions in the same type of the function being compiled 
      // TODO Test recursive call: it could also be the function being compiled
      foreach (var otherFun in function.type.functions)
      {
        if (otherFun.name == identifier.value)
        {
          return otherFun;
        }
      }

      return null;
    }

    private ISymbol FindLocalVariableOrNull(string name)
    {
      foreach (var lv in localVariables)
      {
        if (lv.name == name)
        {
          return lv;
        }
      }
      return null;
    }
    private ISymbol FindFunctionArgumentOrNull(string name)
    {
      foreach (var arg in function.arguments)
      {
        if (arg.name == name)
        {
          return arg;
        }
      }
      return null;
    }
    private bool LocalVariableOrFunctionArgumentExists(string name) {
      return FindLocalVariableOrNull(name) != null || FindFunctionArgumentOrNull(name) != null;
    }

    enum VarOrCallChainCompileState
    {
      Start,
      TypeForStaticAccess,
      CallDone,
      AccessDone
    }

    private void CompileFunctionStatement_VarOrCallChainMaybeAssignStatement(VarOrCallChainMaybeAssignStatementSyntax statementSyntax, List<Statement> statements)
    {
      var varOrCallChain = CompileVarOrCallChain(statementSyntax.varOrCallChainSyntax);

      var lastVarOrCall = varOrCallChain.varOrCalls[varOrCallChain.varOrCalls.Count - 1];
      if (lastVarOrCall.argumentExpressions != null)
      {
        // The statemend ended with a call, it's valid.
        statements.Add(new VarOrCallChainMaybeAssignStatement(varOrCallChain, null));
        return;
      }

      // The statement ended with a varible access, an assign is expected to form a valid statement

      if (statementSyntax.valueToAssignExpression == null)
      {
        throw new CompilerException($"Assign operator expected");
      }

      var assignedSymbol = (DeclarationStatement)(lastVarOrCall.symbolToAccessOrCall);
      if (!assignedSymbol.isVar)
      {
        throw new CompilerException($"Variable {assignedSymbol.name} can't be modified because declared with 'let'");
      }

      // The statemend ended with a call, it's valid.
      var valueToAssignExpression = CompileExpression(statementSyntax.valueToAssignExpression);
      // TODO Check the type assigned agains the variable type
      statements.Add(new VarOrCallChainMaybeAssignStatement(varOrCallChain, valueToAssignExpression));
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

      var identifierName = declarationStatement.identifierToken.value;
      if (LocalVariableOrFunctionArgumentExists(identifierName)) 
      {
        throw new CompilerException($"Variable with name {identifierName} already declared");
      }

      var ds = new DeclarationStatement(declarationStatement.isVar,
                                        type,
                                        identifierName,
                                        initializer);
      localVariables.Add(ds);
      statements.Add(ds);
    }

    private void CompileFunctionStatement_ReturnStatement(ReturnStatementSyntax returnStatement, List<Statement> statements)
    {
      IExpression value = null;

      if (returnStatement.value != null)
      {
        value = CompileExpression(returnStatement.value);
        if (value.Type != function.returnType) 
        {
          if (function.returnType == null)
            throw new CompilerException($"Function must not return a value");
          else
            throw new CompilerException($"Function must return a value of type {function.returnType.name}, not {value.Type.name}");
        }
      } 
      else
      {
        if (function.returnType != null)
          throw new CompilerException($"Function must return a value of type {function.returnType.name}");
      }

      statements.Add(new ReturnStatement(value));
    }

    private void CompileFunctionStatement_IfStatement(IfStatementSyntax ifStatement, List<Statement> statements)
    {
      var value = CompileExpression(ifStatement.condition);
      if (value.Type != typeResolver.boolType) 
      {
        throw new CompilerException($"if condition must produce a boolean value");
      }

      var trueBranchStatements = new List<Statement>();
      CompileStatements(ifStatement.trueBranchStatements, trueBranchStatements);
      var falseBranchStatements = new List<Statement>();
      CompileStatements(ifStatement.falseBranchStatements, falseBranchStatements);

      statements.Add(new IfStatement(value, trueBranchStatements, falseBranchStatements));
    }

    private IExpression CompileExpression(IExpressionSyntax expressionSyntax,
                                          int parentOpPrecedence = int.MaxValue)
    {
      if (expressionSyntax.GetType() == typeof(ExpressionBinOpSyntax))
      {
        var expressionBinOpSyntax = (ExpressionBinOpSyntax)expressionSyntax;
        var left = CompileExpression(expressionBinOpSyntax.leftExpressionSyntax);
        var right = CompileExpression(expressionBinOpSyntax.rightExpressionSyntax);

        Type type;
        switch (expressionBinOpSyntax.op.value)
        {
          case "<":
          case ">":
            type = typeResolver.boolType;
            break;

          default:
            // TODO handle type conversions
            type = left.Type;
            break;
        }

        return new ExpressionBinOp(expressionBinOpSyntax.op,
                                   left,
                                   right,
                                   type);
      }
      else if (expressionSyntax.GetType() == typeof(ExpressionParenthesizedSyntax))
      {
        var expressionParenthesizedSyntax = (ExpressionParenthesizedSyntax)expressionSyntax;
        return new ExpressionParenthesized(CompileExpression(expressionParenthesizedSyntax.expression));
      }
      else if (expressionSyntax.GetType() == typeof(ExpressionLiteralSyntax))
      {
        var expressionLiteralSyntax = (ExpressionLiteralSyntax)expressionSyntax;
        return CompileExpressionLiteral(expressionLiteralSyntax);
      }
      else if (expressionSyntax.GetType() == typeof(VarOrCallChainSyntax))
      {
        var varOrCallChainSyntax = (VarOrCallChainSyntax)expressionSyntax;
        return CompileVarOrCallChain(varOrCallChainSyntax);
      }
      else
      {
        throw new CompilerException($"Could not determine type of expression");
      }
    }

    private IExpression CompileExpressionLiteral(ExpressionLiteralSyntax expressionSyntax)
    {
      Type type = null;
      if (expressionSyntax.literal.type == TokenType.String)
      {
        type = typeResolver.TryResolveTypeByName("string");
      }
      else if (expressionSyntax.literal.type == TokenType.Number)
      {
        // TODO Decimal types
        type = typeResolver.TryResolveTypeByName("int");
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

    private VarOrCallChain CompileVarOrCallChain(VarOrCallChainSyntax varOrCallChain)
    {
      var variableOrCallSyntaxes = varOrCallChain.variableOrCallSyntaxes;
      var state = VarOrCallChainCompileState.Start;
      Type typeForStaticAccess = null;
      var variableOrCalls = new List<VarOrCall>();
      foreach (var variableOrCallSyntax in variableOrCallSyntaxes)
      {
        switch (state)
        {
          case VarOrCallChainCompileState.Start:
            {
              // The first can either be a variable, a function or a type used to call a static method or to access a static variable
              // TODO local variables, function args, instance args or function either static or not
              typeForStaticAccess = typeResolver.TryResolveTypeByTokenValue(variableOrCallSyntax.identifierToken);
              if (typeForStaticAccess != null)
              {
                // TODO Test this case
                if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
                  throw new CompilerException($"{typeForStaticAccess.name} is a type and can't be invoked as a function", variableOrCallSyntax.identifierToken);

                // Ok go on, the next will be a static method or field
                state = VarOrCallChainCompileState.TypeForStaticAccess;
                break;
              }

              var identifier = FindIdentifierOrNull(variableOrCallSyntax.identifierToken);
              if (identifier != null)
              {
                if (variableOrCallSyntax.argumentExpressionSyntaxes != null)
                {
                  // TODO Test this case
                  if (identifier.GetType() != typeof(Function))
                    throw new CompilerException($"{identifier.Name} is not a function", variableOrCallSyntax.identifierToken);

                  // TODO Validate argument number and type agains function signature
                  // TODO Test callability: if the current function is static, it can't call non-static functions!

                  // Evaluate the args
                  var argExprs = new List<IExpression>();
                  foreach (var arg in variableOrCallSyntax.argumentExpressionSyntaxes)
                  {
                    argExprs.Add(CompileExpression(arg));
                  }
                  variableOrCalls.Add(new VarOrCall(identifier, argExprs));

                  state = VarOrCallChainCompileState.CallDone;
                }
                else
                {
                  // TODO Test this case
                  if ((identifier.GetType() != typeof(DeclarationStatement)) && (identifier.GetType() != typeof(FunctionArg)))
                    throw new CompilerException($"{identifier.Name} is not a variable", variableOrCallSyntax.identifierToken);

                  variableOrCalls.Add(new VarOrCall(identifier, null));
                  state = VarOrCallChainCompileState.AccessDone;
                }

                break;
              }

              throw new CompilerException($"Could not resolve {variableOrCallSyntax.identifierToken.value}", variableOrCallSyntax.identifierToken);
            }

          case VarOrCallChainCompileState.TypeForStaticAccess:
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

                // TODO Validate argument number and type agains function signature
                // TODO Test callability: if the current function is static, it can't call non-static functions!

                // Evaluate the args
                var argExprs = new List<IExpression>();
                foreach (var arg in variableOrCallSyntax.argumentExpressionSyntaxes)
                {
                  argExprs.Add(CompileExpression(arg));
                }
                variableOrCalls.Add(new VarOrCall(fun, argExprs));

                state = VarOrCallChainCompileState.CallDone;
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
        case VarOrCallChainCompileState.Start:
          throw new CompilerException($"Empty statement");

        case VarOrCallChainCompileState.TypeForStaticAccess:
          throw new CompilerException($"Member access expected");

        case VarOrCallChainCompileState.CallDone:
          // The statemend ended with a call, it's valid.
          return new VarOrCallChain(variableOrCalls);

        case VarOrCallChainCompileState.AccessDone:
          // The statement ended with a varible access, an assign is expected to form a valid statement
          return new VarOrCallChain(variableOrCalls);

        default:
          throw new CompilerException($"Invalid statement compiler final state: {state}");
      }
    }
  }
}