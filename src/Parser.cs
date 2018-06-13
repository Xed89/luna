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

    private Token nextToken;
    private Token token;
    private int nestingDepth;
    private void NextToken()
    {
      token.Copy(nextToken);
      if (!tokenizer.TryGetNextToken(nextToken))
      {
        nextToken = null;
      }
    }
    private bool HasNextToken()
    {
      return nextToken != null;
    }
    private Token Current()
    {
      var t = new Token();
      t.Copy(token);
      return t;
    }
    private void NestingLevelIncrease()
    {
      nestingDepth += 1;
    }
    private void NestingLevelDecrease()
    {
      if (nestingDepth <= 0)
      {
        throw new ArgumentException("Can't decrease nesting level because it's zero");
      }
      nestingDepth -= 1;
    }

    public SyntaxTree Parse()
    {
      var nodes = new List<SyntaxNode>();

      token = new Token();
      nextToken = new Token();
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
          throw new ArgumentException($"Unexpected token {nextToken.ToString()}");
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
      while (AcceptIndentation(nestingDepth))
      {
        if (AcceptKeyword("fun"))
        {
          functions.Add(ParseFunction());
        }
        else
        {
          throw new ArgumentException($"Unexpected token {nextToken.ToString()}");
        }
      }

      return new TypeDeclarationSyntax(nameToken, functions);
    }

    private FunctionSyntax ParseFunction()
    {
      var funToken = Current();
      var isStatic = AcceptKeyword("static");
      Expect(TokenType.Identifier);
      var identifierToken = Current();
      Expect(TokenType.OpenRoundParenthesis);
      Expect(TokenType.CloseRoundParenthesis);

      TypeSyntax typeSyntax = null;
      if (Accept(TokenType.Colon))
      {
        // TODO
        Expect(TokenType.Identifier);
        var typeToken = Current();
        typeSyntax = new TypeSyntax(typeToken);
      }
      Expect(TokenType.NewLine);

      NestingLevelIncrease();
      var statements = new List<StatementSyntax>();
      // Now we have the function statements!
      while (AcceptIndentation(nestingDepth))
      {
        statements.Add(ParseStatement());
        if (HasNextToken())
        {
          // All statements end with a newline, if the file is not ended
          Expect(TokenType.NewLine);
        }
      }

      return new FunctionSyntax(isStatic, identifierToken, typeSyntax, statements);
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
      else 
      {
        return ParseVarOrCallChainMaybeAssignStatement();
      }
    }

    private VarOrCallChainMaybeAssignStatementSyntax ParseVarOrCallChainMaybeAssignStatement()
    {
      var varOrCallChain = ParseVarOrCallChain();
      //TODO Handle assign
      ExpressionSyntax valueToAssignExpression = null;

      if (Accept(TokenType.Equals))
      {
        valueToAssignExpression = ParseExpression();
      }

      return new VarOrCallChainMaybeAssignStatementSyntax(varOrCallChain, valueToAssignExpression);
    }

    private DeclarationStatementSyntax ParseDeclarationStatement(bool isVar)
    {
      Expect(TokenType.Identifier);
      var identifierToken = Current();
      ExpressionSyntax initializer = null;
      if (Accept(TokenType.Equals))
      {
        initializer = ParseExpression();
      }
      return new DeclarationStatementSyntax(isVar, identifierToken, initializer);
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
      List<ExpressionSyntax> argumentExpressionSyntaxes = null;
      if (Accept(TokenType.OpenRoundParenthesis))
      {
        // Loop until we find a close parenthesis
        var isFirstArg = true;
        argumentExpressionSyntaxes = new List<ExpressionSyntax>();
        
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

    private ExpressionSyntax ParseExpression()
    {
      if (Accept(TokenType.String))
      {
        var literal = Current();
        return new ExpressionSyntax(literal);
      }
      else if (Accept(TokenType.Number))
      {
        var literal = Current();
        return new ExpressionSyntax(literal);
      }
      else if (Accept(TokenType.Identifier))
      {
        //TODO memberAccessExpression
        var literal = Current();
        return new ExpressionSyntax(literal);
      }
      else
      {
        throw new ArgumentException("Invalid expression");
      }
      //TODO handle expression type
    }

    private bool AcceptIndentation(int nestingDepth)
    {
      return Accept(TokenType.Indentation, new String(' ', 2 * nestingDepth));
    }

    private bool AcceptKeyword(String value)
    {
      return Accept(TokenType.Keyword, value);
    }

    private bool Accept(TokenType tokenType, String value = null)
    {
      if ((nextToken == null) || (nextToken.type != tokenType) || (value != null && nextToken.value != value))
      {
        return false;
      }

      NextToken();
      return true;
    }

    private bool Expect(TokenType tokenType)
    {
      if ((nextToken == null) || (nextToken.type != tokenType))
      {
        throw new ArgumentException($"Expected token {tokenType.ToString()}, but found {(nextToken == null ? "null" : nextToken.ToString())}");
      }

      NextToken();
      return true;
    }
  }
}