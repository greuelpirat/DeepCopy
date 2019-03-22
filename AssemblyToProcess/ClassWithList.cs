using System.Collections.Generic;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithList
    {
        public IList<int> List { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithStringList
    {
        public IList<string> List { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithObjectList
    {
        public IList<SomeClass> List { get; set; }
    }
}