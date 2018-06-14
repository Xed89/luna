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

  public class Token
  {
    public readonly TokenType type;
    public readonly string value;
    public string sourceLine;
    public readonly int lineOffset;
    public Token(TokenType type, String value, int lineOffset)
    {
      this.type = type;
      this.value = value;
      this.sourceLine = ""; // Will be set when the line is completed
      this.lineOffset = lineOffset;
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