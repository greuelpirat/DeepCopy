using System;
using DeepCopyConstructor;

namespace SmokeTest
{
    [AddDeepCopyConstructor]
    public class SmokeTestClass
    {
        public int Integer { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public SimpleEnum Enum { get; set; }
        public DemoClass DemoClass { get; set; }
    }

    public enum SimpleEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class DemoClass
    {
        public DemoClass() { }

        public DemoClass(DemoClass source)
        {
            
            if (source.String != null)
                String = String = string.Copy(source.String);
            Float = source.Float;
        }

        public string String { get; set; }
        public float Float { get; set; }
    }
}