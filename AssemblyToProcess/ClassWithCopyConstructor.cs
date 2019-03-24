using System.Collections.Generic;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    public class ClassWithCopyConstructor
    {
        public ClassWithCopyConstructor() { }

        [InjectDeepCopy]
        public ClassWithCopyConstructor(ClassWithCopyConstructor source)
        {
            SpecialObject = new ClassWithNoCopyConstructor { List = new List<int>() };
            for (var i = 0; i < Integer; i++)
                SpecialObject.List.Add(i + 1);
            if (source.SpecialObject?.List != null)
                foreach (var special in source.SpecialObject.List)
                    SpecialObject.List.Add(special);
        }

        public int Integer { get; set; }
        public IList<int> Integers { get; set; }

        [IgnoreDuringDeepCopy] public ClassWithNoCopyConstructor SpecialObject { get; set; }
    }

    public class ClassWithNoCopyConstructor
    {
        public IList<int> List { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithIgnoreDuringDeepCopy
    {
        public int Integer { get; set; }
        [IgnoreDuringDeepCopy] public int IntegerIgnored { get; set; }
        public string String { get; set; }
        [IgnoreDuringDeepCopy] public string StringIgnored { get; set; }
    }
}