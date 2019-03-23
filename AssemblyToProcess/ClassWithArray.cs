using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithArray
    {
        public int[] Array { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithArrayString
    {
        public string[] Array { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithArrayObject
    {
        public SomeObject[] Array { get; set; }
    }
}