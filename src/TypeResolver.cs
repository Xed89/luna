using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class TypeResolver
  {
    public readonly Type boolType;
    private readonly Type stringType;
    private readonly Type intType;
    private readonly Type ConsoleType;
    public TypeResolver()
    {
      boolType = new Type("bool");
      stringType = new Type("string");
      intType = new Type("int");
      ConsoleType = new Type("Console");
      ConsoleType.AddFunction(new Function(ConsoleType, true, "writeLine", new List<FunctionArg>(), null));
    }

    public Type ResolveType(TypeSyntax typeSyntax)
    {
      var type = TryResolveTypeByTokenValue(typeSyntax.typeToken);
      if (type != null)
        return type;

      throw new ArgumentException($"Could not resolve type {typeSyntax.typeToken.value}");
    }

    public Type TryResolveTypeByTokenValue(Token nameToken)
    {
      return TryResolveTypeByName(nameToken.value);
    }

    public Type TryResolveTypeByName(String name)
    {
      // Try with built-in types
      switch (name)
      {
        case "int": return intType;
        case "string": return stringType;
        case "bool": return boolType;
        case "Console": return ConsoleType;
      }

      return null;
    }
  }
}