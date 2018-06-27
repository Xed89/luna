using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class TypeResolver
  {
    public TypeResolver()
    {
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
        case "int": return new Type(name);
        case "string": return new Type(name);
        case "Console":
          {
            var type = new Type(name);
            type.AddFunction(new Function(type, true, "writeLine", new List<FunctionArg>(), null));
            return type;
          }
      }

      return null;
    }
  }
}