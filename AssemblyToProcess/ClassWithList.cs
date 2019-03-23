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
    public class ClassWithListString
    {
        public IList<string> List { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithListObject
    {
        public IList<SomeObject> List { get; set; }
    }
}