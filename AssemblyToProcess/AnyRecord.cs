using System;
using DeepCopy;

namespace AssemblyToProcess;

[AddDeepCopyConstructor(Overwrite = true)]
public record AnyRecord
{
    public int Integer { get; set; }
    public SomeEnum Enum { get; set; }
    public DateTime DateTime { get; set; }
    public string String { get; set; }
    public SomeObject Object { get; set; }
}

public static class AnyRecordExtensions
{
    [DeepCopyExtension]
    public static AnyRecord DeepCopy(this AnyRecord anyRecord) => anyRecord;
}