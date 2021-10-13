using DeepCopy;
using System.ComponentModel;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithBindingList
    {
        public BindingList<string> Strings { get; set; }
    }
}