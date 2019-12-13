```
    /*
    public void WriteAsm(IndentedTextWriter writer)
    {
      writer.WriteLine($"@ Module {name}");
      writer.WriteLine(".text");
      writer.WriteLine(".global _start");
      writer.WriteLine("_start:");
      var typeWithMain = (from t in types
                          where t.GetFunctionByName("main") != null
                          select t).FirstOrDefault();
      writer.WriteLine($"      bl {typeWithMain.name}_main");
      writer.WriteLine($"      swi 0x11   @ halt");
      writer.WriteLine("");
      writer.WriteLine("Console_writeLine:");
      writer.WriteLine("      swi 0x02");
      writer.WriteLine("      ldr r0, =crlf");
      writer.WriteLine("      swi 0x02");
      writer.WriteLine("      bx  lr");
      writer.WriteLine("");
      foreach(var type in types)
      {
        type.WriteAsm(writer);
      }
      writer.WriteLine("");
      writer.WriteLine(".data");
      foreach(var kvp in stringConstants)
      {
        writer.WriteLine($"{kvp.Key}:    .asciz    \"{kvp.Value}\"");
      }
      writer.WriteLine($"crlf:    .asciz    \"\\r\\n\"");
    }
    public void WriteAsm(IndentedTextWriter writer)
    {
      writer.WriteLine($"@ Type {name}");
      foreach(var function in functions)
      {
        function.WriteAsm(writer);
      }
    }
    public void WriteAsm(IndentedTextWriter writer)
    {
      writer.WriteLine($"@ Function {type.name}.{name}");
      writer.WriteLine($"{type.name}_{name}:");
      foreach(var line in asmCode)
      {
        writer.WriteLine($"      {line}");
      }
    }
    */
```