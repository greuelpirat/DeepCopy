using System.Collections.Generic;
using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class DictionaryClass : Dictionary<string, SomeObject>
    {
        public SomeObject SomeProperty { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ListClass : List<SomeObject>
    {
        public SomeObject SomeProperty { get; set; }
    }

    [AddDeepCopyConstructor]
    public class SetClass : HashSet<SomeObject>
    {
        public SomeObject SomeProperty { get; set; }
    }
}