using System.Collections.Generic;
using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithSet
    {
        public ISet<int> Set { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithSetString
    {
        public ISet<string> Set { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithSetObject
    {
        public ISet<SomeObject> Set { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithSetInstance
    {
        public HashSet<SomeObject> Set { get; set; }
    }
}