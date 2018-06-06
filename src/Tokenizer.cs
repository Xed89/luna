using System;
using System.Collections.Generic;
using System.IO;

namespace LunaCompiler
{
  class Tokenizer
  {
    private readonly TextReader input;
    private bool isAtLineStart;
    private readonly HashSet<String> keywords;

    public Tokenizer(TextReader input)
    {
      this.input = input;
      isAtLineStart = false;
      keywords = new HashSet<string>() {"fun", "type"};
    }

    public bool TryGetNextToken(Token token)
    {
      var state = TokenizerState.Begin;
      var tokenType = TokenType.Unknown;
      var accumulator = "";

      while (true)
      {
        var readInt = input.Peek();
        if (readInt == -1)
        {
          return TryFillTokenIfValidAtInputEnd(token, state);
        }

        var readChar = (char)readInt;
        var acceptChar = TokenizerInputCharAccept.Unexpected;
        switch (state)
        {
          case TokenizerState.Begin:
            if (isCharForIdentifierStart(readChar))
            {
              acceptChar = TokenizerInputCharAccept.Accumulate;
              state = TokenizerState.Identifier;

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
              if (isAtLineStart)
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
            else if (readChar == '.')
            {
              acceptChar = TokenizerInputCharAccept.AccumulateAndTokenComplete;
              tokenType = TokenType.Dot;

            }
            else if (readChar == '"')
            {
              acceptChar = TokenizerInputCharAccept.Discard;
              state = TokenizerState.String;

            }
            break;

          case TokenizerState.Indentation:
            if (readChar == ' ')
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
            if (isCharForIdentifier(readChar))
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

          case TokenizerState.String:
            if (readChar == '"')
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
            if (readChar == '\n')
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
            throw new ArgumentException($"Unexpected character: '{readChar}'");

          case TokenizerInputCharAccept.Accumulate:
            accumulator += readChar;
            input.Read();
            break;

          case TokenizerInputCharAccept.AccumulateAndTokenComplete:
            accumulator += readChar;
            input.Read();
            setTokenAndReturnTrue = true;
            break;

          case TokenizerInputCharAccept.Discard:
            input.Read();
            break;

          case TokenizerInputCharAccept.DiscardAndTokenComplete:
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
          isAtLineStart = (tokenType == TokenType.NewLine);
          token.Set(tokenType, accumulator);
          return true;
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
      // TODO
      return false;
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