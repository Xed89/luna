using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  public class CompilerException : Exception
  {
    private String msg;
    public CompilerException(string msg)
    {
      this.msg = msg;
    }
    public CompilerException(string msg, Token token)
    {
      //Underline the current token
      var underline = new string(' ', token.lineOffset) + new string('^', token.value.Length);
      this.msg = msg + Environment.NewLine + token.sourceLine + Environment.NewLine + underline;
    }

    public override string Message
    {
      get
      {
        return msg;
      }
    }
  }
}