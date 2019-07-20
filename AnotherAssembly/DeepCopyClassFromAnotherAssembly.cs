using DeepCopy;

namespace AnotherAssembly
{
    [AddDeepCopyConstructor]
    public class DeepCopyClassFromAnotherAssembly
    {
        public string Text { get; set; }
    }
}