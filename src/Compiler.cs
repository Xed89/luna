using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LunaCompiler
{
  class Compiler
  {
    public readonly SyntaxTree syntaxTree;
    public Compiler(SyntaxTree syntaxTree)
    {
      this.syntaxTree = syntaxTree;
    }

    public Module Compile()
    {
      var types = new List<Type>();

      foreach (var node in syntaxTree.nodes)
      {
        if (node.GetType() == typeof(TypeDeclarationSyntax))
        {
          types.Add(CompileType((TypeDeclarationSyntax)node));
        }
        else
        {
          throw new ArgumentException($"Unexpected syntax {node.GetType().Name}");
        }
      }

      return new Module(syntaxTree.moduleName, types);
    }

    private Type CompileType(TypeDeclarationSyntax typeDeclaration)
    {
      var functions = new List<Function>();

      foreach (var functionSyntax in typeDeclaration.functions)
      {
        functions.Add(CompileFunction(functionSyntax));
      }

      return new Type(typeDeclaration.nameToken.value,
                      functions);
    }

    private Function CompileFunction(FunctionSyntax functionSyntax)
    {
      var returnType = ResolveType(functionSyntax.typeSyntax);
      // TODO Compile statements into function code
      return new Function(functionSyntax.nameToken.value,
                          returnType);
    }

    private Type ResolveType(TypeSyntax typeSyntax)
    {
      // Try with built-in types
      switch (typeSyntax.typeToken.value)
      {
        case "int": return new Type(typeSyntax.typeToken.value, new List<Function>());
      }

      throw new ArgumentException($"Could not resolve type {typeSyntax.typeToken.value}");
    }
  }
}