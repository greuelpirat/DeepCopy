using System;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class SimpleClass
    {
        public SimpleClass() { }

        // ReSharper disable once UnusedParameter.Local
        public SimpleClass(SimpleClass source) { }

        public int Integer { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public SimpleEnum Enum { get; set; }
        public SecondClass SecondClass { get; set; }
    }

    public enum SimpleEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class SecondClass
    {
        public SecondClass() { }

        public SecondClass(SecondClass source)
        {
            String = new string(source.String.ToCharArray());
            Float = source.Float;
        }

        public string String { get; set; }
        public float Float { get; set; }
    }
}