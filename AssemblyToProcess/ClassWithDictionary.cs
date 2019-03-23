using System.Collections.Generic;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithDictionary
    {
        public IDictionary<int, int> Dictionary { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithDictionaryString
    {
        public IDictionary<string, string> Dictionary { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithDictionaryObject
    {
        public IDictionary<SomeKey, SomeObject> Dictionary { get; set; }
    }
}