using DeepCopy;
using System;

namespace AssemblyToProcess
{
    public class ClassWithEmptyCopyConstructor
    {
        public ClassWithEmptyCopyConstructor() { }

        // ReSharper disable once UnusedParameter.Local
        [DeepCopyConstructor] public ClassWithEmptyCopyConstructor(ClassWithEmptyCopyConstructor source) { }

        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
    }
}