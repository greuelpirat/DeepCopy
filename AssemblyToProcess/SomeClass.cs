using System;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class SomeClass
    {
        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
    }

    public enum SomeEnum
    {
        Value1,
        Value2,
        Value3
    }
}