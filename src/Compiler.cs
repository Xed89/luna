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

    private TypeResolver typeResolver;
    private Dictionary<Function, FunctionSyntax> functionToSyntax;

    public Module Compile()
    {
      var types = new List<Type>();
      functionToSyntax = new Dictionary<Function, FunctionSyntax>();
      typeResolver = new TypeResolver();

      foreach (var node in syntaxTree.nodes)
      {
        if (node.GetType() == typeof(TypeDeclarationSyntax))
        {
          types.Add(CompileType((TypeDeclarationSyntax)node));
        }
        else
        {
          throw new CompilerException($"Unexpected syntax {node.GetType().Name}");
        }
      }

      // Now compile all the types function bodies
      foreach(var type in types)
      {
        foreach(var function in type.functions)
        {
          var functionBodyCompiler = new FunctionBodyCompiler(function, functionToSyntax[function], typeResolver);
          functionBodyCompiler.Compile();
        }
      }

      return new Module(syntaxTree.moduleName, types);
    }

    private Type CompileType(TypeDeclarationSyntax typeDeclaration)
    {
      var type = new Type(typeDeclaration.nameToken.value);

      // First compile all the function declarations, then their body
      // Because functions could refer to other functions that come after them
      foreach (var functionSyntax in typeDeclaration.functions)
      {
        var fun = CompileFunctionDeclaration(type, functionSyntax);
        functionToSyntax[fun] = functionSyntax;
        type.AddFunction(fun);
      }

      return type;
    }

    // Current scope data
    private List<DeclarationStatement> localVariables;
    private ISymbol FindIdentifier(Token identifier)
    {
      var result = FindIdentifierOrNull(identifier);
      if (result != null)
        return result;

      throw new CompilerException($"Could not find identifier {identifier.value}", identifier);
    }

    private ISymbol FindIdentifierOrNull(Token identifier)
    {
      // token is an identifier
      foreach (var lv in localVariables)
      {
        if (lv.name == identifier.value)
        {
          return lv;
        }
      }

      return null;
    }

    private Function CompileFunctionDeclaration(Type type, FunctionSyntax functionSyntax)
    {
      Type returnType = null;
      if (functionSyntax.typeSyntax != null)
      {
        returnType = typeResolver.ResolveType(functionSyntax.typeSyntax);
      }

      var arguments = new List<FunctionArg>();
      foreach(var arg in functionSyntax.argumentSyntaxes)
      {
        arguments.Add(new FunctionArg(arg.nameToken.value,
                                      typeResolver.ResolveType(arg.typeSyntax)));
      }

      // TODO determine if the function is static analyzing the variable usage
      // TODO do the same for pure
      var isStatic = true;
      return new Function(type,
                          isStatic,
                          functionSyntax.nameToken.value,
                          arguments,
                          returnType);
    }
  }
}