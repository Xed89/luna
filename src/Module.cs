using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class Module
  {
    public readonly string name;
    public readonly IReadOnlyList<Type> types;
    public Module(string name, IReadOnlyList<Type> types)
    {
      this.name = name;
      this.types = types;
    }

    public void Write(IndentedTextWriter writer)
    {
      writer.WriteLine($"Module {name}");
      writer.Indent += 1;
      foreach(var type in types)
      {
        type.Write(writer);
      }
      writer.Indent -= 1;
    }
  }

  class Type
  {
    public readonly String name;
    public readonly IReadOnlyList<Function> functions;
    public Type(String name, IReadOnlyList<Function> functions)
    {
      this.name = name;
      this.functions = functions;
    }

    public void Write(IndentedTextWriter writer)
    {
      writer.WriteLine($"Type {name}");
      writer.Indent += 1;
      foreach(var function in functions)
      {
        function.Write(writer);
      }
      writer.Indent -= 1;
    }
  }

  class Function
  {
    public readonly String name;
    public readonly Type returnType;
    public Function(String name, Type returnType)
    {
      this.name = name;
      this.returnType = returnType;
    }

    public void Write(IndentedTextWriter writer)
    {
      writer.WriteLine($"Function {name}(): {returnType.name}");
      writer.Indent += 1;
      // TODO Write code
      writer.Indent -= 1;
    }
  }
}