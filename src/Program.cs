using System;

namespace LunaCompiler {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");

      using (var reader = new System.IO.StreamReader(System.IO.File.OpenRead(args[0]))) {
        var tokenizer = new Tokenizer(reader);

        var token = new Token();
        while (tokenizer.TryGetNextToken(token)) {
          Console.WriteLine($"Token: {token.ToString()}");
        }
      }
    }
  }
}
