using System;
using System.IO;

namespace LunaCompiler
{
  public enum TokenType
  {
    Unknown,
    Keyword,
    Identifier,
    Number,
    String,
    OpenRoundParenthesis,
    CloseRoundParenthesis,
    Colon,
    Dot,
    NewLine,
    Indentation,
    Equals,
    Comma
  }

  class Token
  {
    public TokenType type;
    public string value;
    public Token()
    {
      type = TokenType.Unknown;
      value = "";
    }

    public void Set(TokenType type, String value)
    {
      this.type = type;
      this.value = value;
    }

    public void Copy(Token other)
    {
      this.type = other.type;
      this.value = other.value;
    }

    public override string ToString()
    {
      var printValue = (type == TokenType.NewLine || type == TokenType.NewLine) ?
                         value.Replace("\r", "\\r").Replace("\n", "\\n") :
                         value;
      return $"type: {type}, value: '{printValue}'";
    }
  }
}