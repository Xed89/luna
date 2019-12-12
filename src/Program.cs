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
        var testFilter = "";
        if (args.Length > 1) 
        {
          testFilter = args[1];
        }
        var testsPath = System.IO.Path.Combine(System.Environment.CurrentDirectory, "tests");
        var runner = new TestRunner(testsPath, testFilter);
        runner.RunTests();
        return;
      }

      var sourceFullFileName = args[0];
      if (!System.IO.Path.IsPathRooted(sourceFullFileName))
      {
        sourceFullFileName = System.IO.Path.GetFullPath(sourceFullFileName);
      }

      using (var reader = new System.IO.StreamReader(System.IO.File.OpenRead(sourceFullFileName))) {
        var tokenizer = new Tokenizer(reader);
        var moduleName = System.IO.Path.GetFileNameWithoutExtension(sourceFullFileName);
        var parser = new Parser(moduleName, tokenizer);

        var ast = parser.Parse();
      }
    }
  }
}
