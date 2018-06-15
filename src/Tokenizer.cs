using System;
using System.Collections.Generic;
using System.IO;

namespace LunaCompiler
{
  class Tokenizer
  {
    private readonly TextReader input;
    private List<Token> currLineTokens;
    private int idxTokenCurrLine;
    private readonly HashSet<String> keywords;

    public Tokenizer(TextReader input)
    {
      this.input = input;
      this.currLineTokens = new List<Token>();
      this.idxTokenCurrLine = 0;
      keywords = new HashSet<string>() {"fun", "type", "static", "let", "var"};
    }

    public Token GetNextTokenOrNull()
    {
      MaybeTokenizeOneLine();

      if (currLineTokens.Count == 0)
        return null;

      return currLineTokens[idxTokenCurrLine++];
    }

    private void MaybeTokenizeOneLine()
    {
      if (idxTokenCurrLine >= currLineTokens.Count)
      {
        currLineTokens.Clear();
        idxTokenCurrLine = 0;

        string sourceLine = "";
        while (true)
        {
          var token = mTryGetNextToken(ref sourceLine);
          if (token != null)
            currLineTokens.Add(token);

          if ((token == null) || (token.type == TokenType.NewLine))
          {
            //Fix all token lines
            for(int i=0; i<currLineTokens.Count; i++)
              currLineTokens[i].sourceLine = sourceLine;
            
            break;
          }
        }
      }
    }

    public Token mTryGetNextToken(ref string sourceLine)
    {
      var lineOffset = sourceLine.Length;
      var state = TokenizerState.Begin;
      var tokenType = TokenType.Unknown;
      var accumulator = "";

      while (true)
      {
        var readInt = input.Peek();
        var isStreamEnd = (readInt == -1);

        var readChar = (char)readInt;
        var acceptChar = TokenizerInputCharAccept.Unexpected;
        switch (state)
        {
          case TokenizerState.Begin:
            if (isStreamEnd)
            {
              return null;
              
            } else if (isCharForIdentifierStart(readChar))
            {
              acceptChar = TokenizerInputCharAccept.Accumulate;
              state = TokenizerState.Identifier;

            } 
            else if (Char.IsDigit(readChar))
            {
              acceptChar = TokenizerInputCharAccept.Accumulate;
              state = TokenizerState.Number;

            }
            else if (readChar == '\r')
            {
              acceptChar = TokenizerInputCharAccept.Accumulate;
              state = TokenizerState.NewLineCR;

            }
            else if (readChar == '\n')
            {
              // Met a \n, Unix line ending style
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.NewLine;
            }
            else if (readChar == ' ')
            {
              // Note that here we don't handle tabs or other unicode whitespaces, by design
              if (lineOffset == 0)
              {
                // We have an indentation, convert it into a token
                acceptChar = TokenizerInputCharAccept.Accumulate;
                state = TokenizerState.Indentation;
              }
              else
              {
                // It's a whitespace before a token in the middle of the line, ignore it and go on
                acceptChar = TokenizerInputCharAccept.Discard;
              }

            }
            else if (readChar == '(')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.OpenRoundParenthesis;

            }
            else if (readChar == ')')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.CloseRoundParenthesis;

            }
            else if (readChar == ':')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Colon;

            }
            else if (readChar == '=')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Equals;

            }
            else if (readChar == ',')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Comma;

            }
            else if (readChar == '.')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Dot;

            }
            else if (readChar == '+')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Plus;

            }
            else if (readChar == '*')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Asterisk;

            }
            else if (readChar == '"')
            {
              acceptChar = TokenizerInputCharAccept.Discard;
              state = TokenizerState.String;

            }
            break;

          case TokenizerState.Indentation:
            if ((!isStreamEnd) && (readChar == ' '))
            {
              acceptChar = TokenizerInputCharAccept.Accumulate;
            }
            else
            {
              acceptChar = TokenizerInputCharAccept.TokenComplete;
              tokenType = TokenType.Indentation;
            }
            break;

          case TokenizerState.Identifier:
            if ((!isStreamEnd) && isCharForIdentifier(readChar))
            {
              //Keep going!
              acceptChar = TokenizerInputCharAccept.Accumulate;
            }
            else
            {
              // We reached identifier end, stop here
              acceptChar = TokenizerInputCharAccept.TokenComplete;
              tokenType = keywords.Contains(accumulator) ? TokenType.Keyword: TokenType.Identifier;
            }
            break;

          case TokenizerState.Number:
            if ((!isStreamEnd) && Char.IsDigit(readChar))
            {
              //Keep going!
              acceptChar = TokenizerInputCharAccept.Accumulate;
            }
            else
            {
              // TODO Handle decimals
              // TODO Handle type suffix
              acceptChar = TokenizerInputCharAccept.TokenComplete;
              tokenType = TokenType.Number;
            }
            break;

          case TokenizerState.String:
            if (isStreamEnd)
            {
              acceptChar = TokenizerInputCharAccept.Unexpected;
              
            } else if (readChar == '"')
            {
              acceptChar = TokenizerInputCharAccept.DiscardAndTokenComplete;
              tokenType = TokenType.String;
            }
            else
            {
              // It's a char inside the string
              acceptChar = TokenizerInputCharAccept.Accumulate;
            }
            break;

          case TokenizerState.NewLineCR:
            if ((!isStreamEnd) && (readChar == '\n'))
            {
              // Met a \r\n couple, Windows line ending style
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.NewLine;
            }
            else
            {
              // Only \r was met, Commodore line ending style
              acceptChar = TokenizerInputCharAccept.TokenComplete;
              tokenType = TokenType.NewLine;
            }
            break;

          default:
            throw new ArgumentException($"Unexpected state for Tokenizer: '{state}'");
        }

        var setTokenAndReturnTrue = false;
        switch (acceptChar)
        {
          case TokenizerInputCharAccept.Unexpected:
            if (isStreamEnd)
              throw new ArgumentException($"Unexpected end of file");
            else
              throw new ArgumentException($"Unexpected character: '{readChar}'");

          case TokenizerInputCharAccept.Accumulate:
            sourceLine += readChar;
            accumulator += readChar;
            input.Read();
            break;

          case TokenizerInputCharAccept.AccumulateAndTokenComplete:
            sourceLine += readChar;
            accumulator += readChar;
            input.Read();
            setTokenAndReturnTrue = true;
            break;

          case TokenizerInputCharAccept.Discard:
            sourceLine += readChar;
            input.Read();
            break;

          case TokenizerInputCharAccept.DiscardAndTokenComplete:
            sourceLine += readChar;
            input.Read();
            setTokenAndReturnTrue = true;
            break;

          case TokenizerInputCharAccept.TokenComplete:
            setTokenAndReturnTrue = true;
            break;

          default:
            throw new ArgumentException($"Unexpected accept for Tokenizer: '{acceptChar}'");
        }

        if (setTokenAndReturnTrue)
        {
          return new Token(tokenType, accumulator, lineOffset);
        }
      }
    }

    private bool isCharForIdentifierStart(char readChar)
    {
      return Char.IsLetter(readChar) || (readChar == '_');
    }
    private bool isCharForIdentifier(char readChar)
    {
      return isCharForIdentifierStart(readChar) || Char.IsDigit(readChar);
    }

    private bool TryFillTokenIfValidAtInputEnd(Token token, TokenizerState state)
    {
      switch (state)
        {
          case TokenizerState.Begin:
            return false;

          case TokenizerState.Indentation:
            return true;

          case TokenizerState.Identifier:
            return true;

          case TokenizerState.Number:
            return true;

          case TokenizerState.String:
            return false;

          case TokenizerState.NewLineCR:
            return true;

          default:
            throw new ArgumentException($"Unexpected state for Tokenizer: '{state}'");
        }
    }

    private enum TokenizerState
    {
      Begin,
      Identifier,
      Number,
      String,
      NewLineCR,
      Indentation
    }

    private enum TokenizerInputCharAccept
    {
      Unexpected,
      Accumulate,
      AccumulateAndTokenComplete,
      Discard,
      DiscardAndTokenComplete,
      TokenComplete
    }
  }
}