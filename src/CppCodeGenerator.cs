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
      writer.WriteLine($"#include \"stdarg.h\"");
      writer.WriteLine($"class Console {{");
      writer.WriteLine($"public:");
      writer.WriteLine($"  static void writeLine(const char* msg, ...) {{");
      writer.WriteLine($"    va_list argp;");
      writer.WriteLine($"    va_start(argp, msg);");
      writer.WriteLine($"    vfprintf(stdout, msg, argp);");
      writer.WriteLine($"    fprintf(stdout, \"\\n\");");
      writer.WriteLine($"    va_end(argp);");
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
        if (statement.GetType() == typeof(VarOrCallChainMaybeAssignStatement))
        {
          var varOrCallChainMaybeAssignStatement = (VarOrCallChainMaybeAssignStatement)statement;
          var varOrCallChain = varOrCallChainMaybeAssignStatement.varOrCallChain;
          foreach(var varOrCall in varOrCallChain.varOrCalls)
          {
            if (varOrCall.symbolToAccessOrCall.GetType() == typeof(Function))
            {
              var funToCall = (Function)varOrCall.symbolToAccessOrCall;
              writer.Write($"{funToCall.type.name}::{funToCall.name}(");
              var isFirstArg = true;
              foreach(var argExpr in varOrCall.argumentExpressions)
              {
                if (!isFirstArg)
                {
                  writer.Write(", ");
                }
                isFirstArg = false;
                //Assume literal for now
                if (argExpr.type.name == "string")
                {
                  writer.Write($"\"{argExpr.literal.value}\"");
                }
                else
                {
                  writer.Write(argExpr.literal.value);
                }
              }
              writer.Write($")");
            }
            else if (varOrCall.symbolToAccessOrCall.GetType() == typeof(DeclarationStatement))
            {
              var declarationStatement = (DeclarationStatement)varOrCall.symbolToAccessOrCall;
              writer.Write($"{declarationStatement.name}");
            } 
            else
            {
              throw new ArgumentException($"Unknown symbol type: {varOrCall.symbolToAccessOrCall.GetType().Name}");
            }
          }

          if (varOrCallChainMaybeAssignStatement.valueToAssignExpression != null)
          {
            writer.Write($" = {varOrCallChainMaybeAssignStatement.valueToAssignExpression.literal.value}");
          }
        } 
        else if (statement.GetType() == typeof(DeclarationStatement))
        {
          var declarationStatement = (DeclarationStatement)statement;
          // isVar is ignored because the read-only behavior of let is guaranted by the luna compiler checks
          writer.Write($"{declarationStatement.type.name} {declarationStatement.name}");
          if (declarationStatement.initializer != null)
          {
            //Assume string literal for now
            writer.Write($" = {declarationStatement.initializer.literal.value}");
          }
        }
        else
        {
          throw new ArgumentException($"Unknown statement type: {statement.GetType().Name}");
        }
        writer.WriteLine($";");
      }

      writer.Indent -= 1;
      writer.WriteLine($"}}");
    }
  }
}