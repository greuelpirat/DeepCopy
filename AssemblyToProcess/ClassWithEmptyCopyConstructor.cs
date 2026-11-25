using DeepCopy;
using System;

namespace AssemblyToProcess
{
    public class ClassWithEmptyCopyConstructor
    {
        public ClassWithEmptyCopyConstructor() { }

        // ReSharper disable once UnusedParameter.Local
        [DeepCopyConstructor]
        public ClassWithEmptyCopyConstructor(ClassWithEmptyCopyConstructor source) { }

        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
    }

    public record RecordWithEmptyCopyConstructor
    {
        public RecordWithEmptyCopyConstructor() { }

        [DeepCopyConstructor]
        public RecordWithEmptyCopyConstructor(RecordWithEmptyCopyConstructor source) { }

        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }

        public record InnerRecordWithEmptyCopyConstructor
        {
            public InnerRecordWithEmptyCopyConstructor() { }

            [DeepCopyConstructor]
            public InnerRecordWithEmptyCopyConstructor(InnerRecordWithEmptyCopyConstructor source) { }

            public int Integer { get; set; }
            public SomeEnum Enum { get; set; }
            public DateTime DateTime { get; set; }
            public string String { get; set; }
        }
    }
}