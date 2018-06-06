using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LunaCompiler
{
  class TestRunner
  {
    private Stopwatch stopWatch;
    public void RunTests()
    {
      stopWatch = new Stopwatch();
      Console.WriteLine($"Stopwatch: {Stopwatch.IsHighResolution}");
      var testSourceFiles = CollectTestSourceFiles();
      foreach (var testSourceFile in testSourceFiles)
      {
        var testName = System.IO.Path.GetFileNameWithoutExtension(testSourceFile);

        var outputPath = System.IO.Path.GetDirectoryName(testSourceFile);
        var actualOutputFile = System.IO.Path.Combine(outputPath, testName + "_actual.txt");
        var expectedOutputFile = System.IO.Path.Combine(outputPath, testName + "_expected.txt");

        
        try
        {
          RunOneTest(testSourceFile, actualOutputFile);
        }
        catch (Exception ex)
        {
          System.IO.File.WriteAllText(actualOutputFile, ex.ToString());
        }
        
        var durationMillisec = stopWatch.Elapsed.TotalMilliseconds;

        var isSuccessful = AreFilesEqual(expectedOutputFile, actualOutputFile);
        ConsolePrintTestCompleted(testName, isSuccessful, durationMillisec);
      }
    }

    private void RunOneTest(string testSourceFile, string outputFile)
    {
      using (var reader = new System.IO.StreamReader(System.IO.File.OpenRead(testSourceFile)))
      {
        stopWatch.Start();
        var tokenizer = new Tokenizer(reader);
        var parser = new Parser(tokenizer);

        var syntaxTree = parser.Parse();
        stopWatch.Stop();

        using (var streamWriter = new StreamWriter(outputFile))
        {
          using (var writer = new IndentedTextWriter(streamWriter, "  "))
          {
            writer.WriteLine("Parsing completed");
            writer.WriteLine("Syntax Tree:");
            syntaxTree.WriteTree(writer);
          }
        }
      }
    }

    private void ConsolePrintTestCompleted(string testName, bool success, double durationMillisec)
    {
      var prevColor = Console.ForegroundColor;
      if (success)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"✔");
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"✖");
      }
      Console.ForegroundColor = prevColor;
      Console.WriteLine($" {testName} ({durationMillisec:0.00} ms)");
    }

    private IEnumerable<String> CollectTestSourceFiles()
    {
      var path = System.Environment.CurrentDirectory;
      return System.IO.Directory.EnumerateFiles(path, "tests/*.lu");
    }

    private bool AreFilesEqual(String fileExpected, String fileActual)
    {
      var linesExpected = System.IO.File.ReadAllLines(fileExpected);
      var linesActual = System.IO.File.ReadAllLines(fileActual);
      if (linesExpected.Length != linesActual.Length)
        return false;
      
      for(int i=0; i<linesExpected.Length; i++)
      {
        if (linesExpected[i] != linesActual[i])
          return false;
      }

      return true;
    }
  }
}
