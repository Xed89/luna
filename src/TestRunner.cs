using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LunaCompiler
{
  class TestRunner
  {
    private readonly string path;
    private readonly string filter;
    public TestRunner(string path, string filter)
    {
      this.path = path;
      this.filter = filter;
    }

    private struct TestTimes
    {
      public Int64 parseTimeMicrosec;
      public Int64 compileTimeMicrosec;
      public Int64 cppGenTimeMicrosec;
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

        // Delete the actual output file from the previous run
        System.IO.File.Delete(actualOutputFile);

        var times = new TestTimes();
        stopWatch.Restart();
        try
        {
          RunOneTest(testSourceFile, actualOutputFile, ref times);
        }
        catch (Exception ex)
        {
          System.IO.File.AppendAllText(actualOutputFile, ex.ToString());
        }
        stopWatch.Stop();

        var durationMillisec = stopWatch.Elapsed.TotalMilliseconds;

        var isSuccessful = AreFilesEqual(expectedOutputFile, actualOutputFile);
        ConsolePrintTestCompleted(testName, isSuccessful, durationMillisec, ref times);
      }
    }

    private void RunOneTest(string testSourceFile, string outputFile, ref TestTimes times)
    {
      var watch = new QueryPerfCounter();
      using (var reader = new System.IO.StreamReader(System.IO.File.OpenRead(testSourceFile)))
      {

        var fileName = System.IO.Path.GetFileNameWithoutExtension(testSourceFile);
        var tokenizer = new Tokenizer(reader);

        watch.Start();
        CompilerException parserException;
        SyntaxTree syntaxTree;
        try
        {
          var parser = new Parser(fileName, tokenizer);
          syntaxTree = parser.Parse();
          parserException = null;
        }
        catch (CompilerException ex)
        {
          parserException = ex;
          syntaxTree = null;
        }
        watch.Stop();
        times.parseTimeMicrosec = watch.DurationMicrosec;

        // Write parser output
        using (var sw = new StreamWriter(outputFile, append: true))
        {
          using (var writer = new IndentedTextWriter(sw, "  "))
          {
            if (parserException != null)
            {
              writer.WriteLine("Parsing failed:");
              foreach (var l in parserException.Message.Split(Environment.NewLine))
              {
                writer.WriteLine(l);
              }
              writer.WriteLine("");
            }
            else
            {
              writer.WriteLine("Parsing completed");
              writer.WriteLine("Syntax Tree:");
              syntaxTree.WriteTree(writer);
              writer.WriteLine("");
            }
          }
        }

        if (parserException != null)
        {
          return;
        }

        watch.Start();
        var compiler = new Compiler(syntaxTree);
        CompileResult compileResult;
        try
        {
          compileResult = compiler.Compile();
        }
        catch (CompilerException ex)
        {
          compileResult = new CompileResult(false, null, new List<CompilerException>() {ex});
        }
        watch.Stop();
        times.compileTimeMicrosec = watch.DurationMicrosec;

        // Write compiler output
        using (var sw = new StreamWriter(outputFile, append: true))
        {
          using (var writer = new IndentedTextWriter(sw, "  "))
          {
            if (!compileResult.Succeeded)
            {
              writer.WriteLine("Compile failed:");
              foreach (var compilerException in compileResult.Errors) {
                foreach (var l in compilerException.Message.Split(Environment.NewLine))
                {
                  writer.WriteLine(l);
                }
              }
              writer.WriteLine("");
            }
            else
            {
              writer.WriteLine("Compiled module:");
              compileResult.Module.Write(writer);
              writer.WriteLine("");
            }
          }
        }

        if (!compileResult.Succeeded)
        {
          return;
        }

        watch.Start();
        string cppCode;
        using (var sw = new StringWriter())
        {
          using (var writer = new IndentedTextWriter(sw, "  "))
          {
            var gen = new CppCodeGenerator(compileResult.Module, writer);
            gen.Generate();
            cppCode = sw.ToString();
          }
        }
        watch.Stop();
        times.cppGenTimeMicrosec = watch.DurationMicrosec;

        // Save the code to a file, compile it and run
        string programOutput = CompileAndRunCppCodeReturnOutput(cppCode);
        // Write code and run output
        using (var streamWriter = new StreamWriter(outputFile, append: true))
        {
          using (var writer = new IndentedTextWriter(streamWriter, "  "))
          {
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
      batCompileLines.Add($"call \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\Tools\\VsDevCmd.bat\"");
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
      pCompile.StartInfo.RedirectStandardError = true;
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

    private void ConsolePrintTestCompleted(string testName, bool success, double durationMillisec, ref TestTimes times)
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
      Console.WriteLine($"  Parse time: {times.parseTimeMicrosec/1000.0:0.00} ms");
      Console.WriteLine($"  Compile time: {times.compileTimeMicrosec/1000.0:0.00} ms");
      Console.WriteLine($"  Cpp generation time: {times.cppGenTimeMicrosec/1000.0:0.00} ms");
      Console.WriteLine();
    }

    private IEnumerable<String> CollectTestSourceFiles()
    {
      return System.IO.Directory.EnumerateFiles(path, filter + "*.lu");
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

  public class QueryPerfCounter
  {
    [DllImport("KERNEL32")]
    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long lpFrequency);

    private long start;
    private long stop;
    private long frequency;
    double multiplier = 1.0e6;  // usecs / sec

    public QueryPerfCounter()
    {
      QueryPerformanceFrequency(out frequency);
    }

    public void Start()
    {
      QueryPerformanceCounter(out start);
    }

    public void Stop()
    {
      QueryPerformanceCounter(out stop);
    }

    public Int64 DurationMicrosec
    {
      get {
        return stop - start;
      }
    }

    public double DurationSec
    {
      get {
        return ((stop - start) * multiplier) / frequency;
      }
    }
  }
}
