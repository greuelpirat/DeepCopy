using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithArray
    {
        public int[] Array { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithStringArray
    {
        public string[] Array { get; set; }
    }
}