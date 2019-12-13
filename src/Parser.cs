using System;
using System.Collections.Generic;

namespace LunaCompiler
{
  class Parser
  {
    private readonly String moduleName;
    private readonly Tokenizer tokenizer;
    public Parser(String moduleName,
                  Tokenizer tokenizer)
    {
      this.moduleName = moduleName;
      this.tokenizer = tokenizer;
    }

    private Token token;
    private Token nextToken;
    private Token nextToken2;
    private int nestingDepth;
    private void NextToken()
    {
      token = nextToken;
      nextToken = nextToken2;
      nextToken2 = tokenizer.GetNextTokenOrNull();
    }
    private bool HasNextToken()
    {
      return nextToken != null;
    }
    private Token Current()
    {
      return token;
    }
    private void NestingLevelIncrease()
    {
      nestingDepth += 1;
    }
    private void NestingLevelDecrease()
    {
      if (nestingDepth <= 0)
      {
        throw CreateException("Can't decrease nesting level because it's zero");
      }
      nestingDepth -= 1;
    }

    public SyntaxTree Parse()
    {
      var nodes = new List<SyntaxNode>();

      token = null;
      nextToken = null;
      nextToken2 = tokenizer.GetNextTokenOrNull();
      nestingDepth = 0;
      NextToken();

      while (nextToken != null)
      {
        //Console.WriteLine($"Token: {token.ToString()}");
        if (AcceptKeyword("type"))
        {
          nodes.Add(ParseTypeDeclaration());
        }
        else
        {
          throw CreateException($"Unexpected token {nextToken.ToString()}");
        }
      }

      return new SyntaxTree(moduleName, nodes);
    }

    private TypeDeclarationSyntax ParseTypeDeclaration()
    {
      Expect(TokenType.Identifier);
      var nameToken = Current();
      Expect(TokenType.NewLine);

      NestingLevelIncrease();
      var functions = new List<FunctionSyntax>();
      // Now we have the function statements!
      while (HasNextToken())
      {
        // Skip newlines after type and between funs
        // TODO skip also indentation followed by newline even if indentation does not match current level
        if (Accept(TokenType.NewLine))
        {
          continue;
        }

        if (AcceptIndentation(nestingDepth))
        {
          if (AcceptKeyword("fun"))
          {
            functions.Add(ParseFunction());
          }
          else
          {
            throw CreateException($"Unexpected token {nextToken.ToString()}");
          }
        }
        else
        {
          break;
        }
      }
      NestingLevelDecrease();

      return new TypeDeclarationSyntax(nameToken, functions);
    }

    private FunctionSyntax ParseFunction()
    {
      var funToken = Current();
      Expect(TokenType.Identifier);
      var identifierToken = Current();
      Expect(TokenType.OpenRoundParenthesis);

      // Parse arguments
      var args = new List<FunctionArgSyntax>();
      var isFirst = true;
      while (!Accept(TokenType.CloseRoundParenthesis))
      {
        if (!isFirst)
          Expect(TokenType.Comma);
        isFirst = false;

        Expect(TokenType.Identifier);
        var argNameToken = Current();
        Expect(TokenType.Colon);
        var argType = ParseType();
        args.Add(new FunctionArgSyntax(argNameToken, argType));
      }

      TypeSyntax typeSyntax = null;
      if (Accept(TokenType.Colon))
      {
        typeSyntax = ParseType();
      }
      Expect(TokenType.NewLine);

      var statements = ParseStatementBlock();

      return new FunctionSyntax(identifierToken, args, typeSyntax, statements);
    }

    private TypeSyntax ParseType()
    {
      // TODO
      Expect(TokenType.Identifier);
      var typeToken = Current();
      return new TypeSyntax(typeToken);
    }

    private StatementSyntax ParseStatement()
    {
      if (AcceptKeyword("let"))
      {
        return ParseDeclarationStatement(isVar: false);
      }
      else if (AcceptKeyword("var"))
      {
        return ParseDeclarationStatement(isVar: true);
      }
      else if (AcceptKeyword("return"))
      {
        return ParseReturnStatement();
      }
      else if (AcceptKeyword("if"))
      {
        return ParseIfStatement();
      }
      else
      {
        return ParseVarOrCallChainMaybeAssignStatement();
      }
    }

    private VarOrCallChainMaybeAssignStatementSyntax ParseVarOrCallChainMaybeAssignStatement()
    {
      var varOrCallChain = ParseVarOrCallChain();
      //TODO Handle assign
      IExpressionSyntax valueToAssignExpression = null;

      if (Accept(TokenType.Equals))
      {
        valueToAssignExpression = ParseExpression();
      }
      ExpectIfNotEnded(TokenType.NewLine);

      return new VarOrCallChainMaybeAssignStatementSyntax(varOrCallChain, valueToAssignExpression);
    }

    private DeclarationStatementSyntax ParseDeclarationStatement(bool isVar)
    {
      Expect(TokenType.Identifier);
      var identifierToken = Current();
      IExpressionSyntax initializer = null;
      if (Accept(TokenType.Equals))
      {
        initializer = ParseExpression();
      }
      ExpectIfNotEnded(TokenType.NewLine);
      return new DeclarationStatementSyntax(isVar, identifierToken, initializer);
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
      var value = ParseExpression(acceptEmpty: true);
      ExpectIfNotEnded(TokenType.NewLine);
      return new ReturnStatementSyntax(value);
    }

    private IfStatementSyntax ParseIfStatement()
    {
      var condition = ParseExpression();
      Expect(TokenType.NewLine);

      var trueBranchStatements = ParseStatementBlock();

      List<StatementSyntax> falseBranchStatements = null;
      // TODO Allow empty lines or with correct nesting depth between the if block and the else keyword
      if (AcceptIndentationAndKeyword(nestingDepth, "else"))
      {
        if (AcceptKeyword("if"))
        {
          // It's an "else if", parse the remaining like a nested if
          var nestedIf = ParseIfStatement();
          falseBranchStatements = new List<StatementSyntax>() { nestedIf };
        }
        else
        {
          Expect(TokenType.NewLine);
          falseBranchStatements = ParseStatementBlock();
        }
      }

      if (falseBranchStatements == null)
        falseBranchStatements = new List<StatementSyntax>();

      return new IfStatementSyntax(condition, trueBranchStatements, falseBranchStatements);
    }

    private List<StatementSyntax> ParseStatementBlock()
    {
      NestingLevelIncrease();
      var statements = new List<StatementSyntax>();
      // Now we have the function statements!
      while (true)
      {
        if (Accept(TokenType.NewLine))
        {
          // Allow empty lines without indentation
        }
        else if (AcceptIndentation(nestingDepth))
        {
          if (Accept(TokenType.NewLine))
          {
            // An empty line correctly indented, it's ok.
          }
          else
          {
            statements.Add(ParseStatement());
          }
        }
        else
        {
          break;
        }
      }
      NestingLevelDecrease();
      return statements;
    }

    private VarOrCallChainSyntax ParseVarOrCallChain()
    {
      var varOrCallSyntaxes = new List<VarOrCallSyntax>();
      varOrCallSyntaxes.Add(ParseVarOrCall());
      while (Accept(TokenType.Dot))
      {
        varOrCallSyntaxes.Add(ParseVarOrCall());
      }
      return new VarOrCallChainSyntax(varOrCallSyntaxes);
    }

    private VarOrCallSyntax ParseVarOrCall()
    {
      Expect(TokenType.Identifier);
      var identifier = Current();
      List<IExpressionSyntax> argumentExpressionSyntaxes = null;
      if (Accept(TokenType.OpenRoundParenthesis))
      {
        // Loop until we find a close parenthesis
        var isFirstArg = true;
        argumentExpressionSyntaxes = new List<IExpressionSyntax>();

        while (!Accept(TokenType.CloseRoundParenthesis))
        {
          if (!isFirstArg)
          {
            Expect(TokenType.Comma);
          }
          isFirstArg = false;
          argumentExpressionSyntaxes.Add(ParseExpression());
        }
      }

      return new VarOrCallSyntax(identifier, argumentExpressionSyntaxes);
    }

    private IExpressionSyntax ParseExpression(bool acceptEmpty = false)
    {
      var expressions = new List<IExpressionSyntax>();
      var ops = new List<Token>();
      while (true)
      {
        if (expressions.Count > 0)
        {
          if (Accept(TokenType.SmallerThan))
            ops.Add(Current());
          else if (Accept(TokenType.GreaterThan))
            ops.Add(Current());
          else
            break;
        }

        var expr = ParseExpression_PlusMinus(acceptEmpty);
        if (expr == null)
        {
          if (!acceptEmpty)
            throw new CompilerException("No factor found is valid only when empty expression is accepted");
          return null;
        }
        expressions.Add(expr);
      }

      return BuildBinOpSamePrecedenceExprTree_LeftAssociative(expressions, ops);
    }

    private IExpressionSyntax ParseExpression_PlusMinus(bool acceptEmpty = false)
    {
      var expressions = new List<IExpressionSyntax>();
      var ops = new List<Token>();
      while (true)
      {
        if (expressions.Count > 0)
        {
          if (Accept(TokenType.Plus))
            ops.Add(Current());
          else if (Accept(TokenType.Minus))
            ops.Add(Current());
          else
            break;
        }

        var expr = ParseExpression_AsteriskSlash(acceptEmpty);
        if (expr == null)
        {
          if (!acceptEmpty)
            throw new CompilerException("No factor found is valid only when empty expression is accepted");
          return null;
        }
        expressions.Add(expr);
      }

      return BuildBinOpSamePrecedenceExprTree_LeftAssociative(expressions, ops);
    }

    private IExpressionSyntax ParseExpression_AsteriskSlash(bool acceptEmpty)
    {
      var expressions = new List<IExpressionSyntax>();
      var ops = new List<Token>();
      while (true)
      {
        if (expressions.Count > 0)
        {
          if (Accept(TokenType.Asterisk))
            ops.Add(Current());
          else if (Accept(TokenType.Slash))
            ops.Add(Current());
          else
            break;
        }

        var expr = ParseExpressionTerminalsOrNull();
        if (expr == null)
        {
          // If no terminal was alread matched and the caller allows for no expression
          // then return null to signal no expression found
          if (expressions.Count == 0 && acceptEmpty)
            return null;
          else
            throw CreateException("Expression expected");
        }
        expressions.Add(expr);
      }

      return BuildBinOpSamePrecedenceExprTree_LeftAssociative(expressions, ops);
    }

    private IExpressionSyntax BuildBinOpSamePrecedenceExprTree_LeftAssociative(List<IExpressionSyntax> exprs, List<Token> ops)
    {
      if (exprs.Count == 1)
        return exprs[0];

      IExpressionSyntax left = new ExpressionBinOpSyntax(ops[0],
                                                         exprs[0],
                                                         exprs[1]);
      for (var i = 2; i < exprs.Count; i++)
      {
        left = new ExpressionBinOpSyntax(ops[i - 1], left, exprs[i]);
      }
      return left;
    }

    /*
    private IExpressionSyntax BuildBinOpSamePrecedenceExprTree_RightAssociative(List<IExpressionSyntax> exprs, List<Token> ops)
    {
      if (exprs.Count == 1)
        return exprs[0];

      IExpressionSyntax right = new ExpressionBinOpSyntax(ops[ops.Count-1], 
                                                          exprs[exprs.Count-2], 
                                                          exprs[exprs.Count-1]);
      for(var i= exprs.Count-3; i>=0; i--)
      {
        right = new ExpressionBinOpSyntax(ops[i+1], exprs[i], right);
      }
      return right;
    }
    */

    private IExpressionSyntax ParseExpressionTerminalsOrNull()
    {
      if (Accept(TokenType.String))
      {
        var literal = Current();
        return new ExpressionLiteralSyntax(literal);
      }
      else if (Accept(TokenType.Number))
      {
        var literal = Current();
        return new ExpressionLiteralSyntax(literal);
      }
      else if (Peek(TokenType.Identifier))
      {
        return ParseVarOrCallChain();
      }
      else if (Accept(TokenType.OpenRoundParenthesis))
      {
        var expr = ParseExpression();
        Expect(TokenType.CloseRoundParenthesis);
        return new ExpressionParenthesizedSyntax(expr);
      }
      else
      {
        return null;
      }
    }

    private bool AcceptIndentation(int nestingDepth)
    {
      return Accept(TokenType.Indentation, new String(' ', 2 * nestingDepth));
    }
    private bool AcceptIndentationAndKeyword(int nestingDepth, String value)
    {
      if (Peek(TokenType.Indentation, new String(' ', 2 * nestingDepth)) &&
          Peek2(TokenType.Keyword, value))
      {
        NextToken();
        NextToken();
        return true;
      }

      return false;
    }

    private bool AcceptKeyword(String value)
    {
      return Accept(TokenType.Keyword, value);
    }

    private bool Accept(TokenType tokenType, String value = null)
    {
      if (!Peek(tokenType, value))
        return false;

      NextToken();
      return true;
    }

    private bool Peek(TokenType tokenType, String value = null)
    {
      if ((nextToken == null) || (nextToken.type != tokenType) || (value != null && nextToken.value != value))
      {
        return false;
      }

      return true;
    }
    private bool Peek2(TokenType tokenType, String value = null)
    {
      if ((nextToken2 == null) || (nextToken2.type != tokenType) || (value != null && nextToken2.value != value))
      {
        return false;
      }

      return true;
    }

    private void Expect(TokenType tokenType)
    {
      if ((nextToken == null) || (nextToken.type != tokenType))
      {
        throw CreateException($"Expected token {tokenType.ToString()}, but found {(nextToken == null ? "null" : nextToken.ToString())}");
      }

      NextToken();
    }
    private void ExpectIfNotEnded(TokenType tokenType)
    {
      if (HasNextToken())
        Expect(tokenType);
    }

    private CompilerException CreateException(string msg)
    {
      return new CompilerException(msg, token);
    }
  }
}