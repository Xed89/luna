using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace LunaCompiler
{
  class CppCodeGenerator
  {
    private readonly Module module;
    private readonly IndentedTextWriter writer;
    public CppCodeGenerator(Module module, IndentedTextWriter writer)
    {
      this.module = module;
      this.writer = writer;
    }

    public void Generate()
    {
      writer.WriteLine($"#include \"stdio.h\"");
      writer.WriteLine($"class Console {{");
      writer.WriteLine($"public:");
      writer.WriteLine($"  static void writeLine(const char* msg) {{");
      writer.WriteLine($"    printf(msg);");
      writer.WriteLine($"  }}");
      writer.WriteLine($"}};");
      writer.WriteLine($"");

      foreach(var type in module.types)
      {
        Generate(type);
      }

      // Write the entry point
      var funEntryPoint = module.FindEntryPoint();
      if (funEntryPoint == null)
        throw new ArgumentException("Could not find entry point 'main'");

      writer.WriteLine($"int main() {{");
      writer.WriteLine($"  {funEntryPoint.type.name}::{funEntryPoint.name}();");
      writer.WriteLine($"}}");
    }

    public void Generate(Type type)
    {
      writer.WriteLine($"class {type.name} {{");
      writer.WriteLine($"public:");
      writer.Indent += 1;
      foreach(var function in type.functions)
      {
        Generate(function);
      }
      writer.Indent -= 1;
      writer.WriteLine($"}};");
    }

    public void Generate(Function function)
    {
      var returnTypeStr = function.returnType == null ? "void" : function.returnType.name;
      writer.WriteLine($"static {returnTypeStr} {function.name}() {{");
      writer.Indent += 1;
    
      foreach(var statement in function.statements)
      {
        var memberAccess = statement.memberAccess;
        foreach(var varOrCall in memberAccess.variableOrCalls)
        {
          //Assume static function for now
          var funToCall = (Function)varOrCall.symbolToAccessOrCall;
          writer.Write($"{funToCall.type.name}::{funToCall.name}(");
          foreach(var argExpr in varOrCall.argumentExpressions)
          {
            //Assume string literal for now
            writer.Write($"\"{argExpr.literal.value}\"");
          }
          writer.Write($")");
        }
        writer.WriteLine($";");
      }

      writer.Indent -= 1;
      writer.WriteLine($"}}");
    }
  }
}