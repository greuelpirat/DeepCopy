using DeepCopy;

namespace AssemblyToProcess
{
    public class OuterClassObject
    {
        [AddDeepCopyConstructor]
        public class InnerClassObject
        {
            [AddDeepCopyConstructor]
            public class InnerClassOne
            {
                public SomeObject ObjectOne { get; set; }
            }

            [AddDeepCopyConstructor]
            public class InnerClassTwo
            {
                public SomeObject ObjectTwo { get; set; }
            }

            public InnerClassOne One { get; set; }
            public InnerClassTwo Two { get; set; }
        }
    }
}