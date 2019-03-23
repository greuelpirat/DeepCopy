using System.Collections.Generic;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithDictionary
    {
        public IDictionary<int, int> Dictionary { get; set; }
    }

    /*
    [AddDeepCopyConstructor]
    public class ClassWithStringDictionary
    {
        public IDictionary<string, string> Dictionary { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithObjectDictionary
    {
        public IDictionary<SomeClass, SomeClass> Dictionary { get; set; }
    }
    */
}