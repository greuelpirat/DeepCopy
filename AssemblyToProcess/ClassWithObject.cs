using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithObject
    {
        public SomeClass Object { get; set; }
    }
}