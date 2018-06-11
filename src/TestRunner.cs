using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LunaCompiler
{
  class TestRunner
  {
    private readonly string path;
    public TestRunner(string path)
    {
      this.path = path;
    }

    private Stopwatch stopWatch;
    public void RunTests()
    {
      stopWatch = new Stopwatch();

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
        var fileName = System.IO.Path.GetFileNameWithoutExtension(testSourceFile);
        var tokenizer = new Tokenizer(reader);
        var parser = new Parser(fileName, tokenizer);

        var syntaxTree = parser.Parse();
        var compiler = new Compiler(syntaxTree);
        var module = compiler.Compile();

        string cppCode;
        using (var sw = new StringWriter())
        {
          using (var writer = new IndentedTextWriter(sw, "  "))
          {
            var gen = new CppCodeGenerator(module, writer);
            gen.Generate();
            cppCode = sw.ToString();
          }
        }
        string programOutput = CompileAndRunCppCodeReturnOutput(cppCode);

        // Save the code to a file, compile it and run

        stopWatch.Stop();

        using (var streamWriter = new StreamWriter(outputFile))
        {
          using (var writer = new IndentedTextWriter(streamWriter, "  "))
          {
            writer.WriteLine("Parsing completed");
            writer.WriteLine("Syntax Tree:");
            syntaxTree.WriteTree(writer);
            writer.WriteLine("");
            writer.WriteLine("Compiled module:");
            module.Write(writer);
            writer.WriteLine("");
            writer.WriteLine("Generated Cpp code:");
            writer.Write(cppCode);
            writer.WriteLine("");
            writer.WriteLine("Program output:");
            writer.Write(programOutput);
          }
        }
      }
    }

    private string CompileAndRunCppCodeReturnOutput(string cppCode)
    {
      var tempPath = System.IO.Path.Combine(path, "tmpCompile");
      if (!tempPath.Contains("tests\\tmpCompile"))
      {
        throw new ArgumentException("Assert of folder to delete failed");
      }
      System.IO.Directory.CreateDirectory(tempPath);
      var tmpBatCompileFile = System.IO.Path.Combine(tempPath, "tmpBatCompile.bat");
      var tmpCppSourceFile = System.IO.Path.Combine(tempPath, "tmpCppSource.cpp");
      var tmpCppExeFile = System.IO.Path.Combine(tempPath, "tmpCppSource.exe");
      // Delete previous files
      System.IO.File.Delete(tmpBatCompileFile);
      System.IO.File.Delete(tmpCppSourceFile);
      System.IO.File.Delete(tmpCppExeFile);

      System.IO.File.WriteAllText(tmpCppSourceFile, cppCode);

      var batCompileLines = new List<string>();
      batCompileLines.Add($"call \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\Tools\\VsDevCmd.bat\"");
      batCompileLines.Add($"cd \"{tempPath}\"");
      batCompileLines.Add($"cl tmpCppSource.cpp");
      //batCompileLines.Add("pause");
      System.IO.File.WriteAllLines(tmpBatCompileFile, batCompileLines);

      // Compile
      var pCompile = new Process();
      //pCompile.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.14.26428\bin\Hostx86\x86\cl.exe";
      //pCompile.StartInfo.Arguments = $"{tmpCppSourceFile} -o {tmpCppExeFile}";
      //pCompile.StartInfo.Environment["INCLUDE"] =  @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.14.26428\include;C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\include\um;C:\Program Files (x86)\Windows Kits\10\include\10.0.17134.0\ucrt;C:\Program Files (x86)\Windows Kits\10\include\10.0.17134.0\shared;C:\Program Files (x86)\Windows Kits\10\include\10.0.17134.0\um;C:\Program Files (x86)\Windows Kits\10\include\10.0.17134.0\winrt;C:\Program Files (x86)\Windows Kits\10\include\10.0.17134.0\cppwinrt";
      pCompile.StartInfo.FileName = tmpBatCompileFile;
      pCompile.StartInfo.RedirectStandardOutput = true;
      pCompile.Start();
      var pCompileOutput = pCompile.StandardOutput.ReadToEnd();
      if (pCompile.ExitCode != 0)
        return $"Could not compile, {Environment.NewLine}exit code: {pCompile.ExitCode}{Environment.NewLine}output: {Environment.NewLine}{pCompileOutput}";

      var pRun = new Process();
      pRun.StartInfo.FileName = tmpCppExeFile;
      pRun.StartInfo.RedirectStandardOutput = true;
      pRun.Start();

      return pRun.StandardOutput.ReadToEnd();
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
      return System.IO.Directory.EnumerateFiles(path, "*.lu");
    }

    private bool AreFilesEqual(String fileExpected, String fileActual)
    {
      var linesExpected = System.IO.File.ReadAllLines(fileExpected);
      var linesActual = System.IO.File.ReadAllLines(fileActual);
      if (linesExpected.Length != linesActual.Length)
        return false;

      for (int i = 0; i < linesExpected.Length; i++)
      {
        if (linesExpected[i] != linesActual[i])
          return false;
      }

      return true;
    }
  }
}
