using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithObject
    {
        public OtherClass Object { get; set; }
    }

    [AddDeepCopyConstructor]
    public class OtherClass
    {
        public string String { get; set; }
        public float Float { get; set; }
    }
}