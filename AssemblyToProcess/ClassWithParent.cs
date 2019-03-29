using System.Collections.Generic;
using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class BaseClass
    {
        public SomeObject BaseObject { get; set; }
    }

    [AddDeepCopyConstructor]
    public class DerivedClass : BaseClass
    {
        public SomeObject Object { get; set; }
    }

    [AddDeepCopyConstructor]
    public class OtherDerivedClass : BaseClass
    {
        public SomeObject OtherObject { get; set; }
    }

    public abstract class AbstractBaseClass
    {
        public SomeObject BaseObject { get; set; }
    }

    public class AnotherDerivedClass : AbstractBaseClass
    {
        public SomeObject AnotherObject { get; set; }
    }

    public class YetAnotherDerivedClass : AbstractBaseClass
    {
        public SomeObject YetAnotherObject { get; set; }
    }

    public class BaseClassCollection
    {
        public List<AbstractBaseClass> BaseClasses { get; set; }
    }
}