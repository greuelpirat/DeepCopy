using DeepCopy;
using System;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public struct SomeStruct
    {
        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
        public SomeObject Object { get; set; }
    }
    
    [AddDeepCopyConstructor]
    public struct StructWithReference
    {
        [DeepCopyByReference]
        public SomeObject Object { get; set; }
    }
}