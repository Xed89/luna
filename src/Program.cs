using System;

namespace LunaCompiler {
  class Program {
    static void Main(string[] args) {
      if (args.Length == 0)
      {
        Console.WriteLine("Hi :)");
        return;
      }

      if (args[0] == "-runtests")
      {
        var runner = new TestRunner();
        runner.RunTests();
        return;
      }

      using (var reader = new System.IO.StreamReader(System.IO.File.OpenRead(args[0]))) {
        var tokenizer = new Tokenizer(reader);
        var parser = new Parser(tokenizer);

        var ast = parser.Parse();
      }
    }
  }
}
