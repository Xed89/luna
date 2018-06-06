using System;
using System.Collections.Generic;

namespace LunaCompiler
{
  class Parser
  {
    private readonly Tokenizer tokenizer;
    public Parser(Tokenizer tokenizer)
    {
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
        if (AcceptKeyword("fun"))
        {
          nodes.Add(ParseFunction());
        }
        else
        {
          throw new ArgumentException($"Unexpected token {nextToken.ToString()}");
        }
      }

      return new SyntaxTree(nodes);
    }

    private FunctionSyntax ParseFunction()
    {
      var funToken = Current();
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
      }

      return new FunctionSyntax(identifierToken, typeSyntax, statements);
    }

    private StatementSyntax ParseStatement()
    {
      var memberAccessExpression = ParseMemberAccessExpression();
      //TODO Handle assign
      return new StatementSyntax(memberAccessExpression);
    }

    private MemberAccessExpressionSyntax ParseMemberAccessExpression()
    {
      var variableOrCallSyntaxes = new List<VariableOrCallSyntax>();
      variableOrCallSyntaxes.Add(ParseVariableOrCall());
      while (Accept(TokenType.Dot))
      {
        variableOrCallSyntaxes.Add(ParseVariableOrCall());
      }
      return new MemberAccessExpressionSyntax(variableOrCallSyntaxes);
    }

    private VariableOrCallSyntax ParseVariableOrCall()
    {
      Expect(TokenType.Identifier);
      var identifier = Current();
      var argumentExpressionSyntaxes = new List<ExpressionSyntax>();
      if (Accept(TokenType.OpenRoundParenthesis))
      {
        // Loop until we find a close parenthesis
        var isFirstArg = true;
        while (!Accept(TokenType.CloseRoundParenthesis))
        {
          /* TODO
          if (!isFirstArg)
          {
            Expect(TokenType.Comma)
          }
          */
          isFirstArg = false;
          argumentExpressionSyntaxes.Add(ParseExpression());
        }
      }
      
      return new VariableOrCallSyntax(identifier, argumentExpressionSyntaxes);
    }

    private ExpressionSyntax ParseExpression()
    {
      Expect(TokenType.String);
      var literal = Current();
      //TODO Numbers and memberAccessExpression
      return new ExpressionSyntax(literal);
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