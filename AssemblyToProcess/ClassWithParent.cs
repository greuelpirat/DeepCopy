using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class DerivedClass : BaseClass
    {
        public SomeObject Object { get; set; }
    }

    [AddDeepCopyConstructor]
    public class BaseClass
    {
        public SomeObject BaseObject { get; set; }
    }
}