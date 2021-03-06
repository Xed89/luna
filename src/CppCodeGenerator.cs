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

      foreach (var type in module.types)
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
      foreach (var function in type.functions)
      {
        Generate(function);
      }
      writer.Indent -= 1;
      writer.WriteLine($"}};");
    }

    public void Generate(Function function)
    {
      var returnTypeStr = function.returnType == null ? "void" : function.returnType.name;
      var argStrs = from x in function.arguments
                    select $"{x.type.name} {x.name}";
      var argStr = String.Join(", ", argStrs);
      var staticStr = function.isStatic ? "static " : "";
      writer.WriteLine($"{staticStr}{returnTypeStr} {function.name}({argStr}) {{");
      writer.Indent += 1;

      GenerateStatements(function.statements);

      writer.Indent -= 1;
      writer.WriteLine($"}}");
    }

    private void GenerateStatements(List<Statement> statements)
    {
      foreach (var statement in statements)
      {
        if (statement.GetType() == typeof(VarOrCallChainMaybeAssignStatement))
        {
          var varOrCallChainMaybeAssignStatement = (VarOrCallChainMaybeAssignStatement)statement;
          var varOrCallChain = varOrCallChainMaybeAssignStatement.varOrCallChain;
          GenerateVarOrCallChain(varOrCallChain);

          if (varOrCallChainMaybeAssignStatement.valueToAssignExpression != null)
          {
            writer.Write($" = ");
            GenerateExpression(varOrCallChainMaybeAssignStatement.valueToAssignExpression);
          }
          writer.WriteLine($";");
        }
        else if (statement.GetType() == typeof(DeclarationStatement))
        {
          var declarationStatement = (DeclarationStatement)statement;
          // isVar is ignored because the read-only behavior of let is guaranted by the luna compiler checks

          string actualType;
          switch (declarationStatement.type.name)
          {
            case "string":
              actualType = "const char*";
              break;

            default:
              actualType = declarationStatement.type.name;
              break;
          }

          writer.Write($"{actualType} {declarationStatement.name}");
          if (declarationStatement.initializer != null)
          {
            //Assume literal for now
            writer.Write($" = ");
            GenerateExpression(declarationStatement.initializer);
          }
          writer.WriteLine($";");
        }
        else if (statement.GetType() == typeof(ReturnStatement))
        {
          var returnStatement = (ReturnStatement)statement;
          
          if (returnStatement.value != null)
          {
            writer.Write($"return ");
            GenerateExpression(returnStatement.value);
            writer.WriteLine($";");
          } 
          else
          {
            writer.WriteLine($"return;");
          }
        }
        else if (statement.GetType() == typeof(IfStatement))
        {
          var ifStatement = (IfStatement)statement;
          
          writer.Write("if (");
          GenerateExpression(ifStatement.condition);
          writer.WriteLine(") {");

          writer.Indent += 1;
          GenerateStatements(ifStatement.trueBranchStatements);
          writer.Indent -= 1; 

          if (ifStatement.falseBranchStatements.Count > 0) {
            writer.WriteLine("} else {");
            writer.Indent += 1;
            GenerateStatements(ifStatement.falseBranchStatements);
            writer.Indent -= 1; 
          }
          writer.WriteLine("}");
        }
        else
        {
          throw new ArgumentException($"Unknown statement type: {statement.GetType().Name}");
        }
      }
    }

    private void GenerateExpression(IExpression expression)
    {
      if (expression.GetType() == typeof(ExpressionBinOp))
      {
        var expressionBinOp = (ExpressionBinOp)expression;
        GenerateExpression(expressionBinOp.leftExpr);
        writer.Write($" {expressionBinOp.op.value} ");
        GenerateExpression(expressionBinOp.rightExpr);
      }
      else if (expression.GetType() == typeof(ExpressionParenthesized))
      {
        var expressionParenthesized = (ExpressionParenthesized)expression;
        writer.Write("(");
        GenerateExpression(expressionParenthesized.expression);
        writer.Write(")");
      }
      else if (expression.GetType() == typeof(ExpressionLiteral))
      {
        var expressionLiteral = (ExpressionLiteral)expression;
        //Assume literal for now
        if (expressionLiteral.literal.type == TokenType.String)
        {
          writer.Write($"\"{expressionLiteral.literal.value}\"");
        }
        else
        {
          writer.Write(expressionLiteral.literal.value);
        }
      }
      else if (expression.GetType() == typeof(VarOrCallChain))
      {
        var varOrCallChain = (VarOrCallChain)expression;
        GenerateVarOrCallChain(varOrCallChain);
      }
      else
      {
        throw new ArgumentException($"Could not determine type of expression");
      }
    }

    private void GenerateVarOrCallChain(VarOrCallChain varOrCallChain)
    {
      foreach (var varOrCall in varOrCallChain.varOrCalls)
      {
        if (varOrCall.symbolToAccessOrCall.GetType() == typeof(Function))
        {
          var funToCall = (Function)varOrCall.symbolToAccessOrCall;
          writer.Write($"{funToCall.type.name}::{funToCall.name}(");
          var isFirstArg = true;
          foreach (var argExpr in varOrCall.argumentExpressions)
          {
            if (!isFirstArg)
            {
              writer.Write(", ");
            }
            isFirstArg = false;
            GenerateExpression(argExpr);
          }
          writer.Write($")");
        }
        else if (varOrCall.symbolToAccessOrCall.GetType() == typeof(DeclarationStatement))
        {
          var declarationStatement = (DeclarationStatement)varOrCall.symbolToAccessOrCall;
          writer.Write($"{declarationStatement.name}");
        }
        else if (varOrCall.symbolToAccessOrCall.GetType() == typeof(FunctionArg))
        {
          var functionArg = (FunctionArg)varOrCall.symbolToAccessOrCall;
          writer.Write($"{functionArg.name}");
        }
        else
        {
          throw new ArgumentException($"Unknown symbol type: {varOrCall.symbolToAccessOrCall.GetType().Name}");
        }
      }
    }
  }
}