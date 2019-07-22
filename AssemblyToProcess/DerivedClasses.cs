using System.Collections.Generic;
using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithDerivedProperties
    {
        public DictionaryClass Dictionary { get; set; }
        public ListClass List { get; set; }
        public SetClass Set { get; set; }
    }

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