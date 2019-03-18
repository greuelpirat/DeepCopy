using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithArray
    {
        public string[] Array { get; set; }
    }
}