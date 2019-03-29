using System.Collections.Generic;
using DeepCopy;

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
    
    [AddDeepCopyConstructor]
    public class ClassWithListInstance
    {
        public List<SomeObject> List { get; set; }
    }
}