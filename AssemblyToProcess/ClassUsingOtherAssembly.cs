using AnotherAssembly;
using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassUsingOtherAssembly
    {
        public ClassFromAnotherAssembly Property { get; set; }
    }
    
    [AddDeepCopyConstructor]
    public class ClassUsingOtherDeepCopyAssembly
    {
        public DeepCopyClassFromAnotherAssembly Property { get; set; }
    }
}